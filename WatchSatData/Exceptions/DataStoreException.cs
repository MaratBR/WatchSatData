using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WatchSatData.Exceptions
{

    [Serializable]
    public class DataStoreException : WatchSatException
    {
        public DataStoreException() { }
        public DataStoreException(string message) : base(message) { }
        public DataStoreException(string message, Exception inner) : base(message, inner) { }
        protected DataStoreException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
