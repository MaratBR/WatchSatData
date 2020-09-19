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


        public async Task<IEnumerable<DirectoryState>> GetAvailableParentDirectoriesStates()
        {
            return (await GetParentDirectories()).Select(GetState);
        }

        public DirectoryState GetState(DirectoryCleanupConfig config)
        {
            var dirInfo = new DirectoryInfo(config.FullPath);
            return new DirectoryState
            {
                Config = config,
                SubDirectories = dirInfo.GetDirectories().Select(
                    d => new SubDirectoryState
                    {
                        Name = d.Name,
                        FullPath = d.FullName,
                        LastWriteTime = d.LastWriteTime,
                        ExpirationTime = d.LastWriteTime + TimeSpan.FromDays(config.MaxAge)
                    }).ToList()
            };
        }

        public async Task<IEnumerable<SubDirectoryState>> GetSubDirectories()
        {
            var now = DateTime.Now;
            return (await GetAvailableParentDirectoriesStates())
                .SelectMany(s => s.SubDirectories);
        }


        public async Task<IEnumerable<SubDirectoryState>> GetExpiredSubDerectories()
        {
            return from sub in await GetSubDirectories()
                   where sub.IsExpired
                   select sub;
        }

        public async Task<IEnumerable<DirectoryState>> GetExpiredDirectories()
        {
            return (await GetAvailableParentDirectoriesStates())
                .Where(s => s.SubDirectories.Any(sub => sub.IsExpired));
        }

        public async Task<DateTime?> GetNextCleaupTime()
        {
            var now = DateTime.Now;
            return (await GetSubDirectories())
                .Where(s => !s.IsExpired)
                .OrderBy(s => s.ExpirationTime)
                .FirstOrDefault()
                ?.ExpirationTime;
        }
    }
}
