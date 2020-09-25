using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WatcherSatData_UI.Exceptions;

namespace WatcherSatData_UI.ServicesImpl
{

    [Serializable]
    public class ServiceUnavailableException : ServiceException
    {
        public ServiceUnavailableException() { }
        public ServiceUnavailableException(string message) : base(message) { }
        public ServiceUnavailableException(string message, Exception inner) : base(message, inner) { }
        protected ServiceUnavailableException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
