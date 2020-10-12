using System;
using System.Runtime.Serialization;

namespace WatcherSatData_UI.ServicesImpl
{
    [Serializable]
    public class ServiceFaultException : ServiceException
    {
        public ServiceFaultException()
        {
        }

        public ServiceFaultException(string message) : base(message)
        {
        }

        public ServiceFaultException(string message, Exception inner) : base(message, inner)
        {
        }

        protected ServiceFaultException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}