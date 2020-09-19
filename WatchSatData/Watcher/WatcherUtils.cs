using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
