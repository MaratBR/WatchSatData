using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WatcherSatData_UI.Exceptions
{
    [Serializable]
    public class AppBaseException : Exception
    {
        public AppBaseException() { }
        public AppBaseException(string message) : base(message) { }
        public AppBaseException(string message, Exception inner) : base(message, inner) { }
        protected AppBaseException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
