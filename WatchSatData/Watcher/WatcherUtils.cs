using System;
using System.IO;
using WatchSatData.DataStore;

namespace WatchSatData.Watcher
{
    public static class WatcherUtils
    {
        public static bool? IsExpired(DirectoryCleanupConfig record, SubDirectoryState subDirectory)
        {
            if (!Directory.Exists(subDirectory.FullPath))
                return null;
            var lastModified = Directory.GetLastWriteTime(subDirectory.FullPath);
            return ExpirationDate(lastModified, record.MaxAge) < DateTime.Now;
        }

        public static DateTime ExpirationDate(DateTime lastModified, double maxAgeInDays)
        {
            return lastModified.Date.AddDays(maxAgeInDays);
        }
    }
}