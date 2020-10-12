﻿using System;
using System.Runtime.Serialization;

namespace WatchSatData.DataStore
{
    [DataContract]
    public class DirectoryCleanupConfig : ICloneable
    {
        [DataMember] public Guid Id { get; set; } = Guid.NewGuid();

        [DataMember] public string FullPath { get; set; }

        [DataMember] public bool Exists { get; set; }

        [DataMember] public double MaxAge { get; set; }

        public double MaxAgeCorrected => Math.Max(MaxAge, 5f / TimeSpan.FromDays(1).TotalSeconds);

        [DataMember] public string Alias { get; set; }

        [DataMember] public DateTime AddedAt { get; set; } = DateTime.Now;

        [DataMember] public string Filter { get; set; }

        [DataMember] public DateTime? LastCleanupTime { get; set; }

        [DataMember] public CleanupTarget CleanupTarget { get; set; }

        public object Clone()
        {
            return new DirectoryCleanupConfig
            {
                AddedAt = AddedAt,
                FullPath = FullPath,
                Alias = Alias,
                MaxAge = MaxAge,
                Exists = Exists,
                Filter = Filter,
                LastCleanupTime = LastCleanupTime,
                Id = Id,
                CleanupTarget = CleanupTarget
            };
        }

        public static DateTime ExpirationDate(DateTime lastModified, int maxAgeInDays)
        {
            return lastModified.Date.AddDays(maxAgeInDays);
        }
    }
}