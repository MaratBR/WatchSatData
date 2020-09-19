using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WatchSatData.Exceptions
{

    [Serializable]
    public class WatchSatException : Exception
    {
        public WatchSatException() { }
        public WatchSatException(string message) : base(message) { }
        public WatchSatException(string message, Exception inner) : base(message, inner) { }
        protected WatchSatException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
