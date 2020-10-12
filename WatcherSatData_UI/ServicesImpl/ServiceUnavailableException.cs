using System;
using System.Runtime.Serialization;

namespace WatcherSatData_UI.ServicesImpl
{
    [Serializable]
    public class ServiceUnavailableException : ServiceException
    {
        public ServiceUnavailableException()
        {
        }

        public ServiceUnavailableException(string message) : base(message)
        {
        }

        public ServiceUnavailableException(string message, Exception inner) : base(message, inner)
        {
        }

        protected ServiceUnavailableException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}