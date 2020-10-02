using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WatcherSatData_CLI.IO;
using WatchSatData.DataStore;
using WatchSatData.Exceptions;

namespace WatcherSatData_CLI.WatcherImpl
{
    public abstract class AbstractFileDataStore : IDataStore
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private List<DirectoryCleanupConfig> records;
        private AbstractFileDataStoreOptions options;
        private bool needsRefresh;

        public event EventHandler Changed;

        public string Location { get; }

        protected AbstractFileDataStore(string filePath, AbstractFileDataStoreOptions options)
        {
            Location = filePath;
            this.options = options;
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
            Changed?.Invoke(this, EventArgs.Empty);
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
                Changed?.Invoke(this, EventArgs.Empty);
            }
        }

        public async Task UpdateDirectory(DirectoryCleanupConfig directory)
        {
            await EnsureFreshness();
            var index = records.FindIndex(r => r.Id == directory.Id);
            if (index != -1)
            {
                records[index] = (DirectoryCleanupConfig)directory.Clone();
                records[index].Normalize();
                records[index].FullPath = PathUtils.NormalizePath(directory.FullPath);
                await PutAllDataAndHandleExceptions();
                Changed?.Invoke(this, EventArgs.Empty);
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
                throw new PersistenceDataStoreException("Не удалось сохранить данные, проверьте InnerException для подробной информации", exc);
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
                    throw new PersistenceDataStoreException("Не удалось получить данные, проверьте InnerException для подробной информации", exc);
                }
            }
        }

        protected async Task<List<DirectoryCleanupConfig>> GetAllData()
        {
            using (var file = await OpenFile())
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

        private async Task<FileStream> OpenFile()
        {
            var fileNotExists = !File.Exists(Location);
            if (fileNotExists)
            {
                string dir = Path.GetDirectoryName(Location);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
            }
            FileStream file = await FileHelper.OpenFileWithAttemts(Location, FileMode.OpenOrCreate, 5);

            if (fileNotExists)
            {
                var data = Encoding.UTF8.GetBytes("[]");
                await file.WriteAsync(data, 0, data.Length);
                await file.FlushAsync();
                file.SetLength(data.Length);
                file.Seek(0, SeekOrigin.Begin);
            }

            return file;
        }

        protected abstract List<DirectoryCleanupConfig> ConvertFromString(string fileContents);

        protected abstract string ConvertToString(List<DirectoryCleanupConfig> data);
    }
}
