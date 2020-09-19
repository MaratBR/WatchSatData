using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WatchSatData.DataStore;
using WatchSatData.Exceptions;

namespace WatcherSatData_CLI.WatcherImpl
{
    public abstract class AbstractFileDataStore : IDataStore
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private List<DirectoryCleanupConfig> records;
        private AbstractFileDataStoreOptions options;
        private FileSystemWatcher fsWatcher;
        private bool needsRefresh;

        public event EventHandler<DataStoreChangedEventArgs> Changed;

        public string Location { get; }

        protected AbstractFileDataStore(string filePath, AbstractFileDataStoreOptions options)
        {
            Location = filePath;
            this.options = options;

            fsWatcher = new FileSystemWatcher();
            fsWatcher.Path = Path.GetDirectoryName(filePath);
            fsWatcher.Filter = Path.GetFileName(filePath);
            fsWatcher.NotifyFilter = NotifyFilters.LastWrite;
            fsWatcher.Changed += FsWatcher_Changed;
            fsWatcher.EnableRaisingEvents = true;
        }

        private async void FsWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            try
            {
                needsRefresh = true;
                // Это нужно, чтобы исправить странный баг FileSystemWatcher'а, когда событие вызывается дважды
                fsWatcher.EnableRaisingEvents = false;
                await Task.Delay(400);
                Changed?.Invoke(this, new DataStoreChangedEventArgs { Type = DataStoreChangedEventArgs.Change.Refresh });
            }
            finally
            {
                fsWatcher.EnableRaisingEvents = true;
            }
        }

        #region IService implementation

        public async Task CreateDirectory(DirectoryCleanupConfig directory)
        {
            if (directory == null)
                throw new ArgumentNullException(nameof(directory));

            await EnsureFreshness();
            var normalizedPath = PathUtils.NormalizePath(directory.FullPath);
            if (records.Any(r => r.Id == directory.Id))
            {
                throw new DirectoryConfigNotFoundException(directory.Id);
            }

            records.Insert(0, directory);
            await PutAllDataAndHandleExceptions();
        }

        public async Task DeleteDirectory(Guid id)
        {
            await EnsureFreshness();

            int index = records.FindIndex(r => r.Id == id);
            if (index == -1)
            {
                throw new DirectoryConfigNotFoundException(id);
            }
            else
            {
                records.RemoveAt(index);
                await PutAllDataAndHandleExceptions();
            }
        }

        public async Task UpdateDirectory(DirectoryCleanupConfig directory)
        {
            await EnsureFreshness();
            var index = records.FindIndex(r => r.Id == directory.Id);
            if (index != -1)
            {
                records[index] = (DirectoryCleanupConfig)directory.Clone();
                records[index].FullPath = PathUtils.NormalizePath(directory.FullPath);
                await PutAllDataAndHandleExceptions();
            }
            else
            {
                throw new DirectoryConfigNotFoundException(directory.Id);
            }
        }

        public async Task<IEnumerable<DirectoryCleanupConfig>> FindByPath(string path)
        {
            var lowerPath = path.ToLower();
            return (
                from record in await GetAll()
                let simularity = PathUtils.SimularityIndex(record.FullPath.ToLower(), lowerPath)
                where simularity > 0
                orderby simularity descending
                select record
            ).ToList();
        }

        public async Task<IEnumerable<DirectoryCleanupConfig>> GetAll()
        {
            await EnsureFreshness();
            return records;
        }

        public async Task<DirectoryCleanupConfig> GetById(Guid id)
        {
            return (await GetAll()).FirstOrDefault(c => c.Id == id) ?? throw new DirectoryConfigNotFoundException(id);
        }

        #endregion

        private async Task PutAllDataAndHandleExceptions()
        {
            try
            {
                await PutAllData(records);
            }
            catch (Exception exc)
            {
                throw new PersistenceDataStoreException("Failed to persist all data, check inner exception for more info", exc);
            }
        }

        private async Task EnsureFreshness()
        {
            if (needsRefresh || records == null)
            {
                needsRefresh = false;

                try
                {
                    records = await GetAllData();
                }
                catch (Exception exc)
                {
                    throw new PersistenceDataStoreException("Failed to retrieve all data, check inner exception for more info", exc);
                }
            }
        }

        protected async Task<List<DirectoryCleanupConfig>> GetAllData()
        {
            using (var file = await OpenFile(true))
            using (var reader = new StreamReader(file))
            {
                var dateStr = await reader.ReadToEndAsync();
                return ConvertFromString(dateStr);
            }
        }

        protected async Task PutAllData(List<DirectoryCleanupConfig> data)
        {
            using (var file = await OpenFile())
            {
                var dataStr = ConvertToString(data);
                var dataBytes = Encoding.UTF8.GetBytes(dataStr);
                await file.WriteAsync(dataBytes, 0, dataBytes.Length);
                file.Seek(0, SeekOrigin.Begin);
                file.SetLength(dataBytes.Length);
            }
        }

        private async Task<FileStream> OpenFile(bool onlyRead = false)
        {
            var fileNotExists = !File.Exists(Location);
            if (fileNotExists)
            {
                string dir = Path.GetDirectoryName(Location);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
            }
            var file = File.Open(Location, FileMode.OpenOrCreate, onlyRead ? FileAccess.Read : FileAccess.ReadWrite, onlyRead ? FileShare.ReadWrite : FileShare.Read);

            if (fileNotExists)
            {
                var dataStr = ConvertToString(new List<DirectoryCleanupConfig>());
                var data = Encoding.UTF8.GetBytes(dataStr);
                await file.WriteAsync(data, 0, data.Length);
                file.Seek(0, SeekOrigin.Begin);
                file.SetLength(data.Length);
            }

            return file;
        }

        protected abstract List<DirectoryCleanupConfig> ConvertFromString(string fileContents);

        protected abstract string ConvertToString(List<DirectoryCleanupConfig> data);
    }
}
