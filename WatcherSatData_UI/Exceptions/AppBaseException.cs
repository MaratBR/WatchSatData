using System;
using System.Runtime.Serialization;

namespace WatcherSatData_UI.Exceptions
{
    [Serializable]
    public class AppBaseException : Exception
    {
        public AppBaseException()
        {
        }

        public AppBaseException(string message) : base(message)
        {
        }

        public AppBaseException(string message, Exception inner) : base(message, inner)
        {
        }

        protected AppBaseException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}