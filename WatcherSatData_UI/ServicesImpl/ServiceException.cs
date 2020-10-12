using System;
using System.Runtime.Serialization;
using WatcherSatData_UI.Exceptions;

namespace WatcherSatData_UI.ServicesImpl
{
    [Serializable]
    public class ServiceException : AppBaseException
    {
        public ServiceException()
        {
        }

        public ServiceException(string message) : base(message)
        {
        }

        public ServiceException(string message, Exception inner) : base(message, inner)
        {
        }

        protected ServiceException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}