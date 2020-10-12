using System;
using System.Runtime.Serialization;

namespace WatchSatData.Exceptions
{
    [Serializable]
    public class ServiceOperationFailedException : WatchSatException
    {
        public ServiceOperationFailedException()
        {
        }

        public ServiceOperationFailedException(string message) : base(message)
        {
        }

        public ServiceOperationFailedException(string message, Exception inner) : base(message, inner)
        {
        }

        protected ServiceOperationFailedException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}