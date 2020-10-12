using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using WatcherSatData_UI.Services;
using WatchSatData;
using WatchSatData.DataStore;

namespace WatcherSatData_UI.ServicesImpl
{
    class WatcherServiceProxy : IService
    {
        private IService inner;
        private WatcherServiceProvider provider;
        private int retriesCount;
        private bool available = false;
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public event EventHandler<ServiceStateChangedEventArgs> StateChanged;
        public WatcherServiceProxy(WatcherServiceProvider provider, int retriesCount)
        {
            this.retriesCount = retriesCount;
            this.provider = provider;
        }

        public Task Ping()
        {
            return TryNTimesAsync(service => service.Ping());
        }

        private IService GetService()
        {
            if (inner == null)
            {
                return TryNTimes(() =>
                {
                    inner = provider.InstantiateService();
                    return inner;
                });
            }
            return inner;
        }

        public Task CreateDirectory(DirectoryCleanupConfig record)
        {
            return TryNTimesAsync(service => service.CreateDirectory(record));
        }

        public Task DeleteDirectory(Guid id)
        {
            return TryNTimesAsync(service => service.DeleteDirectory(id));
        }

        public Task<IEnumerable<DirectoryCleanupConfig>> FindDirectoriesByPath(string path)
        {
            return TryNTimesAsync(service => service.FindDirectoriesByPath(path));
        }

        public Task<IEnumerable<DirectoryCleanupConfig>> GetAllDirectories()
        {
            return TryNTimesAsync(service => service.GetAllDirectories());
        }

        public Task<DirectoryState> GetDirectoryState(Guid id)
        {
            return TryNTimesAsync(service => service.GetDirectoryState(id));
        }

        public Task<List<DirectoryState>> GetDirectoryStates()
        {
            return TryNTimesAsync(service => service.GetDirectoryStates());
        }

        public Task UpdateDirectory(DirectoryCleanupConfig record)
        {
            return TryNTimesAsync(service => service.UpdateDirectory(record));
        }

        public async Task<T> TryNTimesAsync<T>(Func<IService, Task<T>> func)
        {
            Exception exc = null;
            IService service = GetService();
            for (var i = 0; i < retriesCount; i++)
            {
                try
                {
                    var value = await func(service);
                    OnAvailabilityStatusChanged(true);
                    return value;
                }
                catch (EndpointNotFoundException _exc)
                {
                    OnAvailabilityStatusChanged(false);
                    throw new ServiceUnavailableException("Сервис недоступен", _exc);
                }
                catch (Exception _exc)
                {
                    if (_exc is CommunicationObjectFaultedException)
                    {
                        service = await Reconnect();
                    }
                    exc = _exc;
                }
            }
            inner = null;
            logger.Debug($"Не удалось выполнить операцию за {retriesCount} попыток");
            throw new ServiceFaultException("Произшла ошибка на стороне сервера", exc);
        }

        public async Task TryNTimesAsync(Func<IService, Task> func)
        {
            Exception exc = null;
            IService service = GetService();
            for (var i = 0; i < retriesCount; i++)
            {
                try
                {
                    await func(service);
                    OnAvailabilityStatusChanged(true);
                    return;
                }
                catch (EndpointNotFoundException _exc)
                {
                    OnAvailabilityStatusChanged(false);
                    throw new ServiceUnavailableException("Сервис недоступен", _exc);
                }
                catch (Exception _exc)
                {
                    if (_exc is CommunicationObjectFaultedException)
                    {
                        service = await Reconnect();
                    }
                    exc = _exc;
                }
            }
            inner = null;
            logger.Debug($"Не удалось выполнить операцию за {retriesCount} попыток");
            throw new ServiceFaultException("Произшла ошибка на стороне сервера", exc);
        }

        private void OnAvailabilityStatusChanged(bool v)
        {
            if (available == v)
                return;
            available = v;
            StateChanged?.Invoke(this, new ServiceStateChangedEventArgs(v));
        }

        private async Task<IService> Reconnect()
        {
            inner = null;
            await Task.Delay(150);
            inner = GetService();
            return inner;
        }

        public T TryNTimes<T>(Func<T> func)
        {
            Exception exc = null;
            for (var i = 0; i < retriesCount; i++)
            {
                try
                {
                    return func();
                }
                catch (Exception _exc)
                {
                    exc = _exc;
                }
            }

            throw exc;
        }
    }
}
