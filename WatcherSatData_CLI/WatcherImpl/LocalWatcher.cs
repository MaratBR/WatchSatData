using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WatchSatData;
using WatchSatData.DataStore;
using WatchSatData.Watcher;

namespace WatcherSatData_CLI.WatcherImpl
{
    public class LocalWatcher
    {
        public LocalWatcher(IDataStore store)
        {
            DataStore = store;
        }

        public IDataStore DataStore { get; }

        public async Task<IEnumerable<DirectoryCleanupConfig>> GetParentDirectories()
        {
            return from dirInfo in await DataStore.GetAll()
                   where Directory.Exists(dirInfo.FullPath)
                   select dirInfo;
        }


        public async Task<IEnumerable<DirectoryState>> GetAvailableDirectoriesStates()
        {
            return (await GetParentDirectories()).Select(GetState);
        }

        public DirectoryState GetState(DirectoryCleanupConfig config)
        {
            var dirInfo = new DirectoryInfo(config.FullPath);
            return new DirectoryState
            {
                Config = config,
                ExpirationTime = dirInfo.LastWriteTime.AddDays(config.MaxAge),
                NumberOfChildren = dirInfo.GetDirectories().Length
            };
        }

        public async Task<IEnumerable<DirectoryState>> GetExpiredDirectories()
        {
            return (await GetAvailableDirectoriesStates()).Where(s => s.IsExpired);
        }

        public async Task<DateTime?> GetNextCleaupTime()
        {
            var now = DateTime.Now;
            return (await GetAvailableDirectoriesStates())
                .Where(s => !s.IsExpired)
                .OrderBy(s => s.ExpirationTime)
                .FirstOrDefault()
                ?.ExpirationTime;
        }

        public async Task UpdateExistsValue()
        {
            var values = await DataStore.GetAll();
            var tasks = from v in values
                        let exists = Directory.Exists(v.FullPath)
                        where exists != v.Exists
                        select SetExistsValue(v, exists);
            var tasksList = tasks.ToList();
            await Task.WhenAll(tasksList);
        }

        private Task SetExistsValue(DirectoryCleanupConfig v, bool exists)
        {
            v.Exists = exists;
            return DataStore.UpdateDirectory(v);
        }
    }
}
