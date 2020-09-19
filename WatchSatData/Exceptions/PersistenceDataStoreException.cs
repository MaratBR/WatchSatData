using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WatchSatData.Exceptions
{

    [Serializable]
    public class PersistenceDataStoreException : DataStoreException
    {
        public PersistenceDataStoreException() { }
        public PersistenceDataStoreException(string message) : base(message) { }
        public PersistenceDataStoreException(string message, Exception inner) : base(message, inner) { }
        protected PersistenceDataStoreException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
