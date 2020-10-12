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
        [DataMember] public DirectoryCleanupConfig Config { get; set; }

        [DataMember] public DateTime? ExpirationTime { get; set; }

        [DataMember] public bool Exists { get; set; }

        [DataMember] public bool Unauthorized { get; set; }

        [DataMember] public bool PathTooLong { get; set; }

        [DataMember] public bool InvalidPath { get; set; }

        [DataMember] public int NumberOfChildren { get; set; }

        [DataMember] public int NumberOfFiles { get; set; }

        [DataMember] public int NumberOfSubDirectories { get; set; }

        public bool IsExpired => ExpirationTime != null && ExpirationTime <= DateTime.Now;
    }
}
