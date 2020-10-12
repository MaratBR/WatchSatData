using System;
using System.Runtime.Serialization;
using WatcherSatData_UI.Exceptions;

namespace WatcherSatData_UI.ServicesImpl
{
    [Serializable]
    public class ServiceProviderInvalidException : AppBaseException
    {
        public ServiceProviderInvalidException()
        {
        }

        public ServiceProviderInvalidException(string message) : base(message)
        {
        }

        public ServiceProviderInvalidException(string message, Exception inner) : base(message, inner)
        {
        }

        protected ServiceProviderInvalidException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}