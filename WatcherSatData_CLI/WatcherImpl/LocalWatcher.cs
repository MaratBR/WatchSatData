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
            return (await GetParentDirectories()).Select(GetState).Where(s => s.NumberOfChildren > 0);
        }

        public async Task<TimeSpan> GetSmallestWaitTime()
        {
            DateTime dt;
            try
            {
                dt = (await GetAvailableDirectoriesStates())
                    .Select(d => d.ExpirationTime)
                    .Min();
            }
            catch (InvalidOperationException)
            {
                return TimeSpan.FromMinutes(30);
            }
            var now = DateTime.Now;
            if (dt < now)
                return TimeSpan.Zero;
            return dt - now;
        }

        public DirectoryState GetState(DirectoryCleanupConfig config)
        {
            var dirInfo = new DirectoryInfo(config.FullPath);
            bool hasFilter = !string.IsNullOrWhiteSpace(config.Filter);
            var subDirs = hasFilter ? dirInfo.GetDirectories(config.Filter) : dirInfo.GetDirectories();
            var files = hasFilter ? dirInfo.GetFiles(config.Filter) : dirInfo.GetFiles();
            int count = 0;
            bool subDirsIncluded = config.CleanupTarget == CleanupTarget.All || config.CleanupTarget == CleanupTarget.Directories;
            bool filesIncluded = config.CleanupTarget == CleanupTarget.All || config.CleanupTarget == CleanupTarget.Files;
            if (subDirsIncluded)
                count += subDirs.Length;
            if (config.CleanupTarget == CleanupTarget.All || config.CleanupTarget == CleanupTarget.Files)
                count += files.Length;

            DateTime lastWriteTime = config.LastCleanupTime ?? dirInfo.LastWriteTime;


            if (filesIncluded)
            {
                foreach (var file in files)
                {
                    if (file.LastWriteTime < lastWriteTime)
                        lastWriteTime = file.LastWriteTime;
                }
            }

            if (subDirsIncluded)
            {
                foreach (var dir in subDirs)
                {
                    if (dir.LastWriteTime < lastWriteTime)
                        lastWriteTime = dir.LastWriteTime;
                }
            }

            return new DirectoryState
            {
                Config = config,
                ExpirationTime = lastWriteTime.AddDays(config.MaxAgeCorrected),
                NumberOfChildren = count
            };
        }

        public async Task<IEnumerable<DirectoryState>> GetExpiredDirectories()
        {
            return (await GetAvailableDirectoriesStates()).Where(s => s.IsExpired);
        }

        public async Task<DateTime?> GetNextCleaupTime()
        {
            var now = DateTime.Now;
            var next = (await GetAvailableDirectoriesStates())
                .Where(s => !s.IsExpired)
                .OrderBy(s => s.ExpirationTime)
                .FirstOrDefault()
                ?.ExpirationTime;

            return next == null ? null : (DateTime?)ApplyMinimumLimit((DateTime)next);
        }

        private static DateTime ApplyMinimumLimit(DateTime d)
        {
            if (d - DateTime.Now < TimeSpan.FromMinutes(30))
            {
                return DateTime.Now.AddMinutes(30);
            }

            return d;
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
