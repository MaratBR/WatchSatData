using System;
using System.Runtime.Serialization;

namespace WatchSatData.Exceptions
{
    [Serializable]
    public class PersistenceDataStoreException : DataStoreException
    {
        public PersistenceDataStoreException()
        {
        }

        public PersistenceDataStoreException(string message) : base(message)
        {
        }

        public PersistenceDataStoreException(string message, Exception inner) : base(message, inner)
        {
        }

        protected PersistenceDataStoreException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}