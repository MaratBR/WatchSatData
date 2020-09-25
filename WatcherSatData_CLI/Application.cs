using CommandLine;
using Newtonsoft.Json;
using NLog;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Messaging;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WatcherSatData_CLI.WatcherImpl;
using WatchSatData;
using WatchSatData.DataStore;
using WatchSatData.Watcher;

namespace WatcherSatData_CLI
{
    class Application
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        static bool EnsureSingleInstance()
        {
            Process currentProcess = Process.GetCurrentProcess();

            var runningProcess = (
                from process in Process.GetProcesses()
                where 
                    process.Id != currentProcess.Id && 
                    process.ProcessName.Equals(currentProcess.ProcessName, StringComparison.Ordinal)
                select process
                ).FirstOrDefault();

            return runningProcess != null;
        }

        public class Options
        {
#if DEBUG
            private const bool IS_DEBUG = true;
#else
            private const bool IS_DEBUG = false;
#endif

            [Option('d', "root", Required = false, Default = "%HOMEPATH%\\.watch-sat", HelpText = "Папка с конфигурационным файлом")]
            public string Root { get; set; }

            [Option('c', "cfg", Required = false, Default = "watchSat.json", HelpText = "Имя файла конфигурации")]
            public string ConfigFileName { get; set; }

            [Option('l', "log", Required = false, Default = "watchSat.log", HelpText = "Имя файла журнала")]
            public string LogFileName { get; set; }

            [Option('p', "pretty-cfg", Default = IS_DEBUG)]
            public bool PrettyConfig { get; set; }

            [Option("no-service", Default = false, HelpText = "Не запускает WCF сервис")]
            public bool NoService { get; set; }

            [Option("parent-pid")]
            public int? ParentPid { get; set; }
        }

        private Options options;
        private LocalWatcher watcher;
        private TaskCompletionSource<object> configChangeSource;
        private ServiceHost serviceHost;

        private static string AppId = "birdsWatcher_30c58e1c-300d-4dfb-ae9b-01da83d5c7d6";

        public int Run(Options options)
        {
            this.options = options;
            options.Root = Environment.ExpandEnvironmentVariables(options.Root);

            return RunMain();
        }

        public int RunMain()
        {
            if (EnsureSingleInstance())
            {
                Console.WriteLine("Сервис уже запущен");
                return 1;
            }

            InitLog();

            watcher = new LocalWatcher(
                new JsonDataStore(
                    Path.Combine(options.Root, options.ConfigFileName),
                    new JsonDataStoreOptions
                    {
                        Pretty = options.PrettyConfig
                    })
                );
            watcher.DataStore.Changed += DataStore_Changed;

            var service = new Service(watcher);

            logger.Info($"Началась новая сессия. Параметры: {string.Join(" ", Environment.GetCommandLineArgs().Skip(1))}");

            if (!options.NoService)
            {
                serviceHost = new ServiceHost(service, new Uri($"net.pipe://localhost/{AppId}"));
                serviceHost.AddServiceEndpoint(typeof(IService), new NetNamedPipeBinding(), "v1");
                serviceHost.Open();
                logger.Info("Сервис запущен");
            }

            Console.WriteLine("Нажмите Ctrl-C чтобы завершить работу");
            Console.WriteLine("---------------------------------");
            Console.WriteLine();

            Task mainTask, observerTask = null;

            if (options.ParentPid != null)
                observerTask = ObserveParent();
            mainTask = StartWatcherAsync();

            try
            {
                if (observerTask == null)
                    mainTask.Wait();
                else
                    Task.WaitAll(mainTask, observerTask);
            }
            finally
            {
                Console.WriteLine("Остановка сервиса...");
                serviceHost.Close();
            }

            return 0;
        }

        private async Task ObserveParent()
        {
            while (true)
            {
                try
                {
                    Process.GetProcessById((int)options.ParentPid);
                }
                catch (ArgumentException)
                {
                    logger.Debug("Процесс родитель мёртв, завершаю работу");
                    Environment.Exit(0);
                    return;
                }
                await Task.Delay(30000);
            }
        }

        private void InitLog()
        {
            var config = new LoggingConfiguration();

            var fileTarget = new FileTarget("logfile") 
            {
                AutoFlush = true,
                FileName = Path.Combine(options.Root, options.LogFileName),
                Encoding = Encoding.UTF8
            };
            fileTarget.Layout = new SimpleLayout();

            var consoleTarget = new ConsoleTarget("console");
            consoleTarget.Layout = new SimpleLayout("${longdate}|${level:uppercase=true}|${logger}|Thread-${threadid}|${message}");

            config.AddTarget(fileTarget);
            config.AddTarget(consoleTarget);

            config.AddRule(LogLevel.Trace, LogLevel.Off, consoleTarget);
            config.AddRule(LogLevel.Info, LogLevel.Off, fileTarget);

            LogManager.Configuration = config;
        }

        private void DataStore_Changed(object sender, DataStoreChangedEventArgs e)
        {
            logger.Info("Конфигурация была обновлена");
            if (configChangeSource != null && !configChangeSource.Task.IsCompleted)
            {
                configChangeSource.TrySetResult(new object());
            }
        }

        private async Task StartWatcherAsync()
        {
            IEnumerable<DirectoryState> expired;
            while (true)
            {
                expired = await watcher.GetExpiredDirectories();
                if (expired.Count() == 0)
                {
                    DateTime? nextCleanup = await watcher.GetNextCleaupTime();
                    
                    if (nextCleanup == null)
                    {
                        logger.Debug("Папка на удаление не найдена, повторная проверка через час или при обновлении конфигурации");
                        await Task.WhenAny(
                           WaitForConfigChange(),
                           Task.Delay(TimeSpan.FromHours(1))
                           );
                    }
                    else
                    {
                        logger.Debug($"Следуящая очистка - {nextCleanup}");
                        await Task.WhenAny(
                           WaitForConfigChange(),
                           Task.Delay((DateTime)nextCleanup - DateTime.Now)
                           );
                    }
                }
                else
                {
                    foreach (var dir in expired)
                    {
                        var subDirs = dir.SubDirectories.Where(s => s.IsExpired);
                        logger.Debug($"Очистка {dir.Config.FullPath} - {subDirs.Count()} подпапок ({string.Join(", ", subDirs.Select(sd => sd.Name))})");

                        CleanUpDirectory(dir);
                    }
                }
            }
        }

        private void CleanUpDirectory(DirectoryState record)
        {
            foreach (var sub in record.SubDirectories)
            {
                if (sub.IsExpired)
                    Directory.Delete(sub.FullPath, true);
            }
        }

        private Task WaitForConfigChange()
        {
            if (configChangeSource != null && !configChangeSource.Task.IsCompleted)
            {
                return configChangeSource.Task;
            }

            configChangeSource = new TaskCompletionSource<object>();
            return configChangeSource.Task;
        }
    }
}
