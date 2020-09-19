using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using WatcherSatData_CLI.WatcherImpl;
using WatchSatData;
using WatchSatData.DataStore;
using WatchSatData.Exceptions;

namespace WatcherSatData_CLI
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    class Service : IService
    {
        readonly LocalWatcher watcher;

        public Service(LocalWatcher watcher)
        {
            this.watcher = watcher;
        }

        public Task CreateDirectory(DirectoryCleanupConfig record)
        {
            return watcher.DataStore.CreateDirectory(record);
        }

        public Task DeleteDirectory(Guid id)
        {
            return watcher.DataStore.DeleteDirectory(id);
        }

        public Task<IEnumerable<DirectoryCleanupConfig>> FindDirectoriesByPath(string path)
        {
            return watcher.DataStore.FindByPath(path);
        }

        public Task<IEnumerable<DirectoryCleanupConfig>> GetAllDirectories()
        {
            return watcher.DataStore.GetAll();
        }

        public async Task<DirectoryState> GetDirectoryState(Guid id)
        {
            return watcher.GetState(await watcher.DataStore.GetById(id));
        }

        public Task<IEnumerable<DirectoryState>> GetDirectoryStates()
        {
            return watcher.GetAvailableParentDirectoriesStates();
        }

        public Task UpdateDirectory(DirectoryCleanupConfig record)
        {
            return watcher.DataStore.UpdateDirectory(record);
        }

        public Task Ping() => Task.CompletedTask;
    }
}
