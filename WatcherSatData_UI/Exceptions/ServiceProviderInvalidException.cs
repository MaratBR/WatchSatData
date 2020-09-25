using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WatcherSatData_UI.Exceptions
{
    [Serializable]
    public class ServiceProviderInvalidException : Exception
    {
        public ServiceProviderInvalidException() { }
        public ServiceProviderInvalidException(string message) : base(message) { }
        public ServiceProviderInvalidException(string message, Exception inner) : base(message, inner) { }
        protected ServiceProviderInvalidException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
