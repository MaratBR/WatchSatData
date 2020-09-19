using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WatchSatData.Exceptions
{

    [Serializable]
    public class ServiceOperationFailedException : WatchSatException
    {
        public ServiceOperationFailedException() { }
        public ServiceOperationFailedException(string message) : base(message) { }
        public ServiceOperationFailedException(string message, Exception inner) : base(message, inner) { }
        protected ServiceOperationFailedException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
