using System;
using System.Runtime.Serialization;

namespace WatchSatData.Exceptions
{
    [Serializable]
    public class WatchSatException : Exception
    {
        public WatchSatException()
        {
        }

        public WatchSatException(string message) : base(message)
        {
        }

        public WatchSatException(string message, Exception inner) : base(message, inner)
        {
        }

        protected WatchSatException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}