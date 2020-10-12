using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Threading.Tasks;
using WatchSatData.DataStore;
using WatchSatData.Exceptions;

namespace WatchSatData
{
    [ServiceContract]
    public interface IService
    {
        /// <summary>
        ///     Возвращает список конфигураций для директорий
        /// </summary>
        /// <exception cref="Exceptions.PersistenceDataStoreException"></exception>
        /// <returns>Список директорий</returns>
        [OperationContract]
        [FaultContract(typeof(PersistenceDataStoreException))]
        Task<IEnumerable<DirectoryCleanupConfig>> GetAllDirectories();


        /// <summary>
        ///     Возвращает список конфигураций для директорий с похожим путём
        /// </summary>
        /// <exception cref="Exceptions.PersistenceDataStoreException"></exception>
        /// <returns>Список директорий</returns>
        [OperationContract]
        [FaultContract(typeof(PersistenceDataStoreException))]
        Task<IEnumerable<DirectoryCleanupConfig>> FindDirectoriesByPath(string path);


        /// <summary>
        ///     Удаляет конфигурацию
        /// </summary>
        /// <param name="id">ID конфигурации</param>
        /// <exception cref="Exceptions.PersistenceDataStoreException"></exception>
        /// <exception cref="Exceptions.DirectoryConfigNotFoundException"></exception>
        [OperationContract]
        [FaultContract(typeof(PersistenceDataStoreException))]
        [FaultContract(typeof(DirectoryConfigNotFoundException))]
        Task DeleteDirectory(Guid id);

        /// <summary>
        ///     Обновляет конфигурацию
        /// </summary>
        /// <param name="record">Конфигурация, ID должен бытть указан</param>
        /// <exception cref="Exceptions.PersistenceDataStoreException"></exception>
        /// <exception cref="Exceptions.DirectoryConfigNotFoundException"></exception>
        /// <returns>Список директорий</returns>
        [OperationContract]
        [FaultContract(typeof(PersistenceDataStoreException))]
        [FaultContract(typeof(DirectoryConfigNotFoundException))]
        Task UpdateDirectory(DirectoryCleanupConfig record);

        /// <summary>
        ///     Создаёт конфигурацию директории
        /// </summary>
        /// <exception cref="Exceptions.PersistenceDataStoreException"></exception>
        /// <exception cref="Exceptions.DirectoryConfigNotFoundException"></exception>
        [OperationContract]
        [FaultContract(typeof(PersistenceDataStoreException))]
        [FaultContract(typeof(DirectoryConfigNotFoundException))]
        Task CreateDirectory(DirectoryCleanupConfig record);

        /// <exception cref="Exceptions.PersistenceDataStoreException"></exception>
        /// <returns>
        ///     Список объектов, описывающих состояние директорий и их конфигурацию
        /// </returns>
        [OperationContract]
        [FaultContract(typeof(PersistenceDataStoreException))]
        Task<List<DirectoryState>> GetDirectoryStates();

        /// <exception cref="Exceptions.PersistenceDataStoreException"></exception>
        /// <exception cref="Exceptions.DirectoryConfigNotFoundException"></exception>
        /// <returns>
        ///     Объект, описывающий состояние директории и её конфигурацию
        /// </returns>
        [OperationContract]
        [FaultContract(typeof(PersistenceDataStoreException))]
        [FaultContract(typeof(DirectoryConfigNotFoundException))]
        Task<DirectoryState> GetDirectoryState(Guid id);

        [OperationContract]
        Task Ping();
    }

    public static class ServiceExtensions
    {
        public static Task CreateDirectory(this IService service, Action<DirectoryCleanupConfig> init)
        {
            var r = new DirectoryCleanupConfig();
            init(r);
            return service.CreateDirectory(r);
        }
    }
}