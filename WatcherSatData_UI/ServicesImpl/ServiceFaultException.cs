using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WatcherSatData_UI.ServicesImpl
{

    [Serializable]
    public class ServiceFaultException : ServiceException
    {
        public ServiceFaultException() { }
        public ServiceFaultException(string message) : base(message) { }
        public ServiceFaultException(string message, Exception inner) : base(message, inner) { }
        protected ServiceFaultException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
