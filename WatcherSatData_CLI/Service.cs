using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using WatchSatData;
using WatchSatData.DataStore;

namespace WatcherSatData_CLI
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    internal class Service : IService
    {
        private readonly IDataStore ds;
        private readonly TimeSpan minAge;

        public Service(IDataStore dataStore, TimeSpan minAge)
        {
            ds = dataStore;
            this.minAge = minAge;
        }

        private DirectoryState GetState(DirectoryCleanupConfig config)
        {
            var state = new DirectoryState
            {
                Config = config
            };
            DirectoryInfo directoryInfo;

            try
            {
                directoryInfo = new DirectoryInfo(config.FullPath);
            }
            catch (PathTooLongException)
            {
                state.PathTooLong = true;
                return state;
            }
            catch (ArgumentException)
            {
                state.InvalidPath = true;
                return state;
            }

            state.Exists = directoryInfo.Exists;

            if (directoryInfo.Exists)
            {
                DateTime? lastWriteTime = null;

                try
                {
                    lastWriteTime = Directory.GetLastWriteTime(config.FullPath);
                }
                catch (UnauthorizedAccessException e)
                {
                    state.Unauthorized = true;
                }

                var lastCleanupTime = config.LastCleanupTime ?? lastWriteTime ?? DateTime.MinValue;
                var correctedMaxAgeInDays = Math.Max(config.MaxAge, minAge.TotalDays);
                var expiration = lastCleanupTime.AddDays(correctedMaxAgeInDays);
                state.ExpirationTime = expiration;

                var hasFilter = !string.IsNullOrWhiteSpace(config.FullPath);

                var includeFiles = config.CleanupTarget == CleanupTarget.All ||
                                   config.CleanupTarget == CleanupTarget.Files;
                var includeSubDirectories = config.CleanupTarget == CleanupTarget.All ||
                                            config.CleanupTarget == CleanupTarget.Directories;

                if (includeFiles)
                {
                    var files = hasFilter ? directoryInfo.GetFiles(config.Filter) : directoryInfo.GetFiles();
                    state.NumberOfFiles = files.Length;
                }

                if (includeSubDirectories)
                {
                    var subDirs = hasFilter
                        ? directoryInfo.GetDirectories(config.Filter)
                        : directoryInfo.GetDirectories();
                    state.NumberOfSubDirectories = subDirs.Length;
                }

                state.NumberOfChildren = state.NumberOfSubDirectories + state.NumberOfFiles;
            }

            return state;
        }

        #region IService implementation

        public Task CreateDirectory(DirectoryCleanupConfig record)
        {
            return ds.CreateDirectory(record);
        }

        public Task DeleteDirectory(Guid id)
        {
            return ds.DeleteDirectory(id);
        }

        public Task<IEnumerable<DirectoryCleanupConfig>> FindDirectoriesByPath(string path)
        {
            return ds.FindByPath(path);
        }

        public Task<IEnumerable<DirectoryCleanupConfig>> GetAllDirectories()
        {
            return ds.GetAll();
        }

        public async Task<DirectoryState> GetDirectoryState(Guid id)
        {
            return GetState(await ds.GetById(id));
        }

        public async Task<List<DirectoryState>> GetDirectoryStates()
        {
            return (await GetAllDirectories())
                .Select(GetState)
                .ToList();
        }

        public Task UpdateDirectory(DirectoryCleanupConfig record)
        {
            return ds.UpdateDirectory(record);
        }

        public Task Ping()
        {
            return Task.CompletedTask;
        }

        #endregion
    }
}