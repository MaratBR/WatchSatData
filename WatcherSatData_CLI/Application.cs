using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using NLog;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;
using WatcherSatData_CLI.WatcherImpl;
using WatchSatData;
using WatchSatData.DataStore;
using WatchSatData.Exceptions;

namespace WatcherSatData_CLI
{
    internal class Application
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static readonly TimeSpan MinAgeTimeSpan = TimeSpan.FromSeconds(5);

        private const string AppId = "birdsWatcher_30c58e1c-300d-4dfb-ae9b-01da83d5c7d6";
        private TaskCompletionSource<object> configChangeSource;
        private IDataStore ds;

        private Options options;
        private IService service;
        private ServiceHost serviceHost;

        private static bool EnsureSingleInstance()
        {
            var currentProcess = Process.GetCurrentProcess();

            var runningProcess = (
                from process in Process.GetProcesses()
                where
                    process.Id != currentProcess.Id &&
                    process.ProcessName.Equals(currentProcess.ProcessName, StringComparison.Ordinal)
                select process
            ).FirstOrDefault();

            return runningProcess != null;
        }

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

            ds = new JsonDataStore(
                Path.Combine(options.Root, options.ConfigFileName),
                new JsonDataStoreOptions
                {
                    Pretty = options.PrettyConfig
                });
            ds.Changed += DataStore_Changed;

            service = new Service(ds, MinAgeTimeSpan);

            Logger.Info(
                $"Началась новая сессия. Параметры: {string.Join(" ", Environment.GetCommandLineArgs().Skip(1))}");

            if (!options.NoService)
            {
                serviceHost = new ServiceHost(service, new Uri($"net.pipe://localhost/{AppId}"));
                serviceHost.AddServiceEndpoint(typeof(IService), new NetNamedPipeBinding(), "v1");
                serviceHost.Open();
                Logger.Info("Сервис запущен");
            }

            Console.WriteLine("Нажмите Ctrl-C чтобы завершить работу");
            Console.WriteLine("---------------------------------");
            Console.WriteLine();

            Task observerTask = null;

            if (options.ParentPid != null)
                observerTask = ObserveParent();
            var mainTask = StartWatcherAsync();

            try
            {
                if (observerTask == null)
                    try
                    {
                        mainTask.Wait();
                    }
                    catch (Exception exc)
                    {
                        Logger.Error($"Неожиданная ошибка: {exc}");
                        throw exc;
                    }
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
                configChangeSource.TrySetResult(new object());
        }

        private async Task ObserveParent()
        {
            while (true)
            {
                try
                {
                    Process.GetProcessById((int) options.ParentPid);
                }
                catch (ArgumentException)
                {
                    Logger.Debug("Процесс родитель мёртв, завершаю работу");
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
            consoleTarget.Layout =
                new SimpleLayout("${longdate}|${level:uppercase=true}|${logger}|Thread-${threadid}|${message}");

            config.AddTarget(fileTarget);
            config.AddTarget(consoleTarget);

            config.AddRule(LogLevel.Trace, LogLevel.Fatal, consoleTarget);
            config.AddRule(LogLevel.Info, LogLevel.Fatal, fileTarget);
            config.AddRule(LogLevel.Trace, LogLevel.Fatal, debugFileTarget);

            LogManager.Configuration = config;
        }


        private async Task StartWatcherAsync()
        {
            var invalidOpLastTime = false;
            IEnumerable<DirectoryState> expired;
            while (true)
            {
                try
                {
                    expired = await GetExpiredDirectories();
                    expired = expired.ToList();
                    invalidOpLastTime = false;
                }
                catch (InvalidOperationException exc)
                {
                    // на случай если обновление конфигурации произошло только что
                    await Task.Delay(500);
                    if (invalidOpLastTime)
                    {
                        Logger.Error($"Не удалось записать/прочитать файл, повторная попытка через 30 сек: {exc}");
                        await Task.Delay(30000);
                    }
                    else
                    {
                        invalidOpLastTime = true;
                    }

                    continue;
                }
                catch (PersistenceDataStoreException exc)
                {
                    invalidOpLastTime = false;
                    Logger.Error(
                        $"Не удалось записать/прочитать файл, повторная попытка через 30 сек: {(exc.InnerException == null ? exc.Message : exc.InnerException.Message)}");
                    await Task.Delay(30000);
                    continue;
                }
                catch (Exception exc)
                {
                    Logger.Error(
                        $"Неожиданная ошибка при получении конфигурации, повторная попытка через 30 сек: {exc}");
                    continue;
                }

                if (!expired.Any())
                {
                    var nextCleanup = await GetNextCleanupTimeOrNull();

                    if (nextCleanup == null)
                    {
                        var sleepTime = await GetSmallestSleepTime();

                        Logger.Debug(
                            $"Папка на удаление не найдена (вероятно конфигурация пуста), повторная проверка через {sleepTime} или при обновлении конфигурации");
                        await Task.WhenAny(
                            WaitForConfigChange(),
                            Task.Delay(sleepTime)
                        );
                    }
                    else
                    {
                        Logger.Debug($"Следующая очистка - {nextCleanup}");
                        var delayMs = Math.Min(
                            Math.Max(1, ((DateTime) nextCleanup - DateTime.Now).TotalMilliseconds),
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
                        if (dir.NumberOfChildren != 0)
                            Logger.Info(
                                $"Очистка {dir.Config.FullPath} - {dir.NumberOfChildren} подпапок  ({dir.NumberOfSubDirectories}) и файлов ({dir.NumberOfFiles})");

                        CleanUpDirectory(dir);
                    }

                    await Task.Delay(1000);
                }
            }
        }

        private async void CleanUpDirectory(DirectoryState record)
        {
            string[] subDirs = null, files = null;
            var hasFilter = !string.IsNullOrWhiteSpace(record.Config.Filter);

            try
            {
                if (record.Config.CleanupTarget == CleanupTarget.Directories ||
                    record.Config.CleanupTarget == CleanupTarget.All)
                    subDirs = hasFilter
                        ? Directory.GetDirectories(record.Config.FullPath, record.Config.Filter)
                        : Directory.GetDirectories(record.Config.FullPath);

                if (record.Config.CleanupTarget == CleanupTarget.Files ||
                    record.Config.CleanupTarget == CleanupTarget.All)
                    files = hasFilter
                        ? Directory.GetFiles(record.Config.FullPath, record.Config.Filter)
                        : Directory.GetFiles(record.Config.FullPath);
            }
            catch
            {
                Logger.Debug($"Не удалось получить список подпапок/файлов {record.Config.FullPath}");
                return;
            }

            var d = (DirectoryCleanupConfig) record.Config.Clone();
            d.LastCleanupTime = DateTime.Now;

            try
            {
                await ds.UpdateDirectory(d);
            }
            catch (DirectoryConfigNotFoundException)
            {
                Logger.Error(
                    $"Не удалось обновить поле LastCleanupTime конфигурации {d.Id}, скорее всего файл конфигурации был обновлен извне, и директория {d.FullPath} удалена из конф., это не помешает работе программы.");
            }
            catch (PersistenceDataStoreException exc)
            {
                Logger.Error($"Ошибка при обновлении данных: {exc}");
            }


            if ((files == null || !files.Any()) && (subDirs == null || !subDirs.Any()))
                return;

            if (subDirs != null)
                foreach (var sub in subDirs)
                    try
                    {
                        Directory.Delete(sub, true);
                    }
                    catch (Exception exc)
                    {
                        Logger.Error($"Не удалось удалить папку {sub}: {exc.Message}");
                    }

            if (files != null)
                foreach (var file in files)
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception exc)
                    {
                        Logger.Error($"Не удалось удалить файл {file}: {exc.Message}");
                    }
        }

        private Task WaitForConfigChange()
        {
            if (configChangeSource != null && !configChangeSource.Task.IsCompleted) return configChangeSource.Task;

            configChangeSource = new TaskCompletionSource<object>();
            return configChangeSource.Task;
        }

        public class Options
        {
#if DEBUG
            private const bool IS_DEBUG = true;
#else
            private const bool IS_DEBUG = false;
#endif

            [Option('d', "root", Required = false, Default = "%APPDATA%\\SatDataWatcher\\Service",
                HelpText = "Папка с конфигурационным файлом")]
            public string Root { get; set; }

            [Option('c', "cfg", Required = false, Default = "watchSat.json", HelpText = "Имя файла конфигурации")]
            public string ConfigFileName { get; set; }

            [Option('l', "log-file", Required = false, Default = "watchSat.log", HelpText = "Имя файла журнала")]
            public string LogFileName { get; set; }

            [Option('L', "d-log-file", Required = false, Default = "watchSat.log.DEBUG",
                HelpText = "Имя файла журнала отладки")]
            public string DebugLogFileName { get; set; }

            [Option('p', "pretty-cfg", Default = IS_DEBUG)]
            public bool PrettyConfig { get; set; }

            [Option("no-service", Default = false, HelpText = "Не запускает WCF сервис")]
            public bool NoService { get; set; }

            [Option("parent-pid")] public int? ParentPid { get; set; }
        }

        #region Helper methods

        private async Task<List<DirectoryState>> GetExpiredDirectories()
        {
            var list = await service.GetDirectoryStates();
            return list.Where(state => state.IsExpired).ToList();
        }

        private async Task<DateTime?> GetNextCleanupTimeOrNull()
        {
            var states = await service.GetDirectoryStates();
            states = states.Where(state => state.Exists && !state.IsExpired).ToList();
            if (!states.Any())
                return null;

            var expiration = states
                .OrderBy(state => state.ExpirationTime)
                .First()
                .ExpirationTime;

            return expiration;
        }

        private async Task<TimeSpan> GetSmallestSleepTime()
        {
            var states = await service.GetDirectoryStates();
            states = states.Where(state => state.Exists).ToList();
            if (!states.Any())
                return MinAgeTimeSpan;

            var expiration = states
                .OrderBy(state => state.ExpirationTime)
                .First()
                .ExpirationTime;

            var ts = DateTime.Now - (DateTime) expiration;
            return ts < MinAgeTimeSpan ? MinAgeTimeSpan : ts;
        }

        #endregion
    }
}