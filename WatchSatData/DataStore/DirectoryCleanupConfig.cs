using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace WatchSatData.DataStore
{
    [DataContract]
    public class DirectoryCleanupConfig : ICloneable
    {
        [DataMember]
        public Guid Id { get; set; } = Guid.NewGuid();

        [DataMember]
        public string FullPath { get; set; }

        [DataMember]
        public bool Exists { get; set; }

        [DataMember]
        public double MaxAge { get; set; }

        [DataMember]
        public string Alias { get; set; }

        [DataMember]
        public DateTime AddedAt { get; set; } = DateTime.Now;

        [DataMember]
        public string Filter { get; set; }

        public static DateTime ExpirationDate(DateTime lastModified, int maxAgeInDays)
        {
            return lastModified.Date.AddDays(maxAgeInDays);
        }

        public object Clone() => new DirectoryCleanupConfig
        {
            AddedAt = AddedAt,
            FullPath = FullPath,
            Alias = Alias,
            MaxAge = MaxAge,
            Exists = Exists,
            Filter = Filter,
            Id = Id
        };
    }
}
