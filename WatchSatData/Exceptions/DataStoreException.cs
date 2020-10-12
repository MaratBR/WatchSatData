using System;
using System.Runtime.Serialization;

namespace WatchSatData.Exceptions
{
    [Serializable]
    public class DataStoreException : WatchSatException
    {
        public DataStoreException()
        {
        }

        public DataStoreException(string message) : base(message)
        {
        }

        public DataStoreException(string message, Exception inner) : base(message, inner)
        {
        }

        protected DataStoreException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}