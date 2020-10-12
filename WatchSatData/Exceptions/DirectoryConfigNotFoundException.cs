using System;
using System.Runtime.Serialization;
using WatchSatData.DataStore;

namespace WatchSatData.Exceptions
{
    [Serializable]
    public class DirectoryConfigNotFoundException : WatchSatException
    {
        public DirectoryConfigNotFoundException()
        {
        }

        public DirectoryConfigNotFoundException(Guid id) : base(
            $"{nameof(DirectoryCleanupConfig)} with id={id} not found")
        {
        }

        public DirectoryConfigNotFoundException(Guid id, Exception inner) : base(
            $"{nameof(DirectoryCleanupConfig)} with id={id} not found", inner)
        {
        }

        protected DirectoryConfigNotFoundException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}