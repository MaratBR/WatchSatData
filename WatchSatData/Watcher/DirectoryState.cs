using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace WatchSatData.DataStore
{
    [DataContract]
    public class DirectoryState
    {
        [DataMember]
        public DirectoryCleanupConfig Config { get; set; }

        [DataMember]
        public DateTime ExpirationTime { get; set; }

        [DataMember]
        public int NumberOfChildren { get; set; }

        public bool IsExpired => ExpirationTime <= DateTime.Now + TimeSpan.FromDays(Config.MaxAge) + TimeSpan.FromSeconds(2);
    }
}
