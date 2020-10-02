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
using WatchSatData.Exceptions;
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

            [Option('d', "root", Required = false, Default = "%APPDATA%\\SatDataWatcher\\Service", HelpText = "Папка с конфигурационным файлом")]
            public string Root { get; set; }

            [Option('c', "cfg", Required = false, Default = "watchSat.json", HelpText = "Имя файла конфигурации")]
            public string ConfigFileName { get; set; }

            [Option('l', "log-file", Required = false, Default = "watchSat.log", HelpText = "Имя файла журнала")]
            public string LogFileName { get; set; }

            [Option('L', "d-log-file", Required = false, Default = "watchSat.log.DEBUG", HelpText = "Имя файла журнала отладки")]
            public string DebugLogFileName { get; set; }

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

            Directory.CreateDirectory(options.Root);

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

        private void DataStore_Changed(object sender, EventArgs e)
        {
            if (configChangeSource != null && !configChangeSource.Task.IsCompleted)
            {
                configChangeSource.TrySetResult(new object());
            }
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
            var layout = new SimpleLayout("${longdate}|${level:uppercase=true}|${logger}|${message}");

            var fileTarget = new FileTarget("logfile") 
            {
                AutoFlush = true,
                FileName = Path.Combine(options.Root, options.LogFileName),
                Encoding = Encoding.UTF8
            };
            fileTarget.Layout = layout;

            var debugFileTarget = new FileTarget("logfile")
            {
                AutoFlush = true,
                FileName = Path.Combine(options.Root, options.DebugLogFileName),
                Encoding = Encoding.UTF8
            };
            debugFileTarget.Layout = layout;


            var consoleTarget = new ConsoleTarget("console");
            consoleTarget.Layout = new SimpleLayout("${longdate}|${level:uppercase=true}|${logger}|Thread-${threadid}|${message}");

            config.AddTarget(fileTarget);
            config.AddTarget(consoleTarget);

            config.AddRule(LogLevel.Trace, LogLevel.Fatal, consoleTarget);
            config.AddRule(LogLevel.Info, LogLevel.Fatal, fileTarget);
            config.AddRule(LogLevel.Trace, LogLevel.Fatal, debugFileTarget);

            LogManager.Configuration = config;
        }


        private async Task StartWatcherAsync()
        {
            IEnumerable<DirectoryState> expired;
            while (true)
            {
                try
                {
                    expired = await watcher.GetExpiredDirectories();
                    expired = expired.ToList();
                }
                catch (InvalidOperationException)
                {
                    // на случай если обновление конфигурации произошло только что
                    await Task.Delay(500);

                    continue;
                }
                catch (PersistenceDataStoreException exc)
                {
                    logger.Error($"Не удалось записать/прочитать файл, повторная попытка через 30 сек: {(exc.InnerException == null ? exc.Message : exc.InnerException.Message)}");
                    await Task.Delay(30000);
                    continue;
                }

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
                        logger.Debug($"Следующая очистка - {nextCleanup}");
                        var delayMs = Math.Min(
                            Math.Max(1, ((DateTime)nextCleanup - DateTime.Now).TotalMilliseconds),
                            int.MaxValue
                            );
                        await Task.WhenAny(
                           WaitForConfigChange(),
                           Task.Delay(TimeSpan.FromMilliseconds(delayMs))
                           );
                    }
                }
                else
                {
                    foreach (var dir in expired)
                    {
                        logger.Info($"Очистка {dir.Config.FullPath} - {dir.NumberOfChildren} подпапок");

                        CleanUpDirectory(dir);
                    }
                }
            }
        }

        private async void CleanUpDirectory(DirectoryState record)
        {
            string[] subDirs;

            try
            {
                subDirs = Directory.GetDirectories(record.Config.FullPath);
            }
            catch
            {
                logger.Debug($"Не удалось получить список подпапок {record.Config.FullPath}");
                return;
            }

            foreach (var sub in subDirs)
            {
                try
                {
                    Directory.Delete(sub);
                }
                catch (Exception exc)
                {
                    logger.Error($"Не удалось удалить папку {sub}: {exc.Message}");
                }
            }
            var d = (DirectoryCleanupConfig)record.Config.Clone();
            d.LastCleanupTime = DateTime.Now;

            try
            {
                await watcher.DataStore.UpdateDirectory(d);
            }
            catch (DirectoryConfigNotFoundException)
            {
                logger.Error($"Не удалось обновить поле LastCleanupTime конфигурации {d.Id}, скорее всего файл конфигурации был обновлен извне, и директория {d.FullPath} удалена из конф., это не помешает работе программы.");
            }
            catch (PersistenceDataStoreException exc)
            {
                logger.Error($"Ошибка при обновлении данных: {exc}");
            }

            
            try
            {
                Directory.SetLastWriteTime(record.Config.FullPath, DateTime.Now);
            }
            catch (Exception)
            {
                logger.Debug("Не удалось обновить LastWriteTime");
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
