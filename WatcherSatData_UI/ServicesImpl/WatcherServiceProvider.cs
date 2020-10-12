using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.ServiceModel;
using System.Threading.Tasks;
using NLog;
using WatcherSatData_UI.Services;
using WatcherSatData_UI.Utils.Proc;
using WatchSatData;

namespace WatcherSatData_UI.ServicesImpl
{
    internal class WatcherServiceProvider : IWatcherServiceProvider
    {
        private static readonly string[] ServiceExe = {"satWatcher.exe", "WatcherSatData_CLI.exe"};
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private Supervisor embedServiceSupervisor;
        private bool isDisposed;
        private bool isInitialized;
        private ServiceState lastState = ServiceState.Default;
        private Task pinger;

        private WatcherServiceProxy service;

        public event EventHandler<ServiceStateChangedEventArgs> StateChanged;

        public IService GetService()
        {
            ThrowIfNotInitializedOrDisposed();
            return service;
        }

        public async Task InitAsync()
        {
            if (isInitialized)
                throw new ServiceProviderInvalidException("ServiceProvider уже инициализирован");

            logger.Debug("Инициализация...");
            isInitialized = true;

            InitService();
            StateChanged?.Invoke(this, new ServiceStateChangedEventArgs(ServiceState.ExpectingOnline));

            if (!await CheckAvailability())
            {
                logger.Debug("Сервис не запущен, запускаю встроенный сервис...");
                CreateEmbedService();
            }
            else
            {
                logger.Debug("Сервис запущен внешне");
            }
        }

        public bool IsEmbed()
        {
            return embedServiceSupervisor != null;
        }

        public ServiceState GetLastState()
        {
            return lastState;
        }

        public void Dispose()
        {
            if (isDisposed)
                return;
            isDisposed = true;
            embedServiceSupervisor?.Dispose();
        }

        private void InitService()
        {
            var proxy = new WatcherServiceProxy(this, 5);
            proxy.StateChanged += WatcherServiceProxy_StateChanged;
            service = proxy;

            pinger = PingLoop();
        }

        private async Task PingLoop()
        {
            while (true)
            {
                // CheckAvailability вызывает метод Ping на proxy сервиса, если метод падает с ошибкой ServiceUnavailable,
                // тригирится событие обновления состояния сервиса (состояние меняется на Offline)
                await CheckAvailability();
                await Task.Delay(2500);
            }
        }

        private void CreateEmbedService()
        {
            if (embedServiceSupervisor != null)
                return; // TODO Exception

            var exe = GetServiceExeFileOrNull();

            if (exe == null)
            {
                logger.Error("Не удалось найти путь к исполняемому файлу сервиса");
                return;
            }

            logger.Error($"Файл сервиса найден: {exe}");


            var parent = Process.GetCurrentProcess();

            embedServiceSupervisor = new Supervisor(new ProcessStartInfo
            {
                FileName = exe,
                Arguments = $"--parent-pid {parent.Id}",
                CreateNoWindow = true,
                UseShellExecute = false
            });
            embedServiceSupervisor.Start();
        }

        private string GetServiceExeFileOrNull()
        {
            foreach (var exe in ServiceExe)
            {
                var fullPath = GetFullPathOrNull(exe);
                if (fullPath != null)
                    return fullPath;
            }

            return null;
        }

        private async Task<bool> CheckAvailability()
        {
            try
            {
                await service.Ping();
                return true;
            }
            catch (ServiceUnavailableException)
            {
                return false;
            }
            catch (ServiceFaultException)
            {
                return true;
            }
        }

        private void WatcherServiceProxy_StateChanged(object sender, ServiceStateChangedEventArgs e)
        {
            logger.Debug($"Статус сервиса: {e.State}");
            lastState = e.State;
            StateChanged?.Invoke(this, e);
        }

        private void ThrowIfNotInitializedOrDisposed()
        {
            if (!isInitialized)
                throw new ServiceProviderInvalidException("ServiceProvider is not initialized");

            if (isDisposed)
                throw new ServiceProviderInvalidException("ServiceProvider is disposed");
        }

        public IService InstantiateService()
        {
            try
            {
                var binding = new NetNamedPipeBinding
                {
                    Security = new NetNamedPipeSecurity {Mode = NetNamedPipeSecurityMode.Transport}
                };
                var endpoint =
                    new EndpointAddress("net.pipe://localhost/birdsWatcher_30c58e1c-300d-4dfb-ae9b-01da83d5c7d6/v1");
                var channelFactory = new ChannelFactory<IService>(binding, endpoint);
                return channelFactory.CreateChannel();
            }
            catch
            {
                return null;
            }
        }

        public static string GetFullPathOrNull(string fileName)
        {
            if (File.Exists(fileName))
                return fileName;

            var values = Environment.GetEnvironmentVariable("PATH");
            foreach (var path in values.Split(Path.PathSeparator))
            {
                var fullPath = Path.Combine(path, fileName);
                if (File.Exists(fullPath))
                    return fullPath;
            }

            var root = Directory.GetParent(Assembly.GetEntryAssembly().Location).FullName;
            var exe = Path.Combine(root, fileName);
            if (File.Exists(exe))
                return exe;

            return null;
        }
    }
}