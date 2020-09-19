using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace WatchSatData.DataStore
{
    [DataContract]
    public class SubDirectoryState
    {
        [DataMember]
        public string FullPath { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public DateTime LastWriteTime { get; set; }

        [DataMember]
        public DateTime ExpirationTime { get; set; }

        public bool IsExpired => ExpirationTime < DateTime.Now;
    }
}
