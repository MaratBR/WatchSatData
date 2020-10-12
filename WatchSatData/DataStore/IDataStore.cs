using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WatchSatData.DataStore
{
    public interface IDataStore
    {
        string Location { get; }

        /// <summary>
        ///     Удаляет конфигурацию у казанный ID
        /// </summary>
        /// <param name="id">Идентификатор конфигурации</param>
        /// <exception cref="Exceptions.DirectoryConfigNotFoundException"></exception>
        /// <exception cref="Exceptions.PersistenceDataStoreException"></exception>
        Task DeleteDirectory(Guid id);


        /// <summary>
        ///     Создаёт запись в конфигурации
        /// </summary>
        /// <param name="directory">Конфигурация</param>
        /// <exception cref="Exceptions.PersistenceDataStoreException"></exception>
        Task CreateDirectory(DirectoryCleanupConfig directory);

        /// <param name="id">Идентификатор конфигурации</param>
        /// <returns>Конфигурация директории</returns>
        /// <exception cref="Exceptions.DirectoryConfigNotFoundException"></exception>
        /// <exception cref="Exceptions.PersistenceDataStoreException">Если не удалось сохранить изменения</exception>
        Task UpdateDirectory(DirectoryCleanupConfig directory);

        /// <param name="id">Идентификатор конфигурации</param>
        /// <returns>Список конфигураций, с похожим путём к папке</returns>
        /// <exception cref="Exceptions.PersistenceDataStoreException"></exception>
        Task<IEnumerable<DirectoryCleanupConfig>> FindByPath(string path);

        /// <param name="id">Идентификатор конфигурации</param>
        /// <returns>Конфигурация директории</returns>
        /// <exception cref="Exceptions.DirectoryConfigNotFoundException"></exception>
        Task<DirectoryCleanupConfig> GetById(Guid id);


        /// <exception cref="Exceptions.PersistenceDataStoreException"></exception>
        /// <returns>Список конфигураций директорий</returns>
        Task<IEnumerable<DirectoryCleanupConfig>> GetAll();

        event EventHandler Changed;
    }
}