using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WatchSatData.DataStore;

namespace WatchSatData.Exceptions
{

    [Serializable]
    public class DirectoryConfigNotFoundException : WatchSatException
    {
        public DirectoryConfigNotFoundException() { }
        public DirectoryConfigNotFoundException(Guid id) : base($"{nameof(DirectoryCleanupConfig)} with id={id} not found") { }
        public DirectoryConfigNotFoundException(Guid id, Exception inner) : base($"{nameof(DirectoryCleanupConfig)} with id={id} not found", inner) { }
        protected DirectoryConfigNotFoundException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
