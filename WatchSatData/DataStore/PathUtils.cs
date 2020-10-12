using System;
using System.IO;
using System.Linq;

namespace WatchSatData.DataStore
{
    public static class PathUtils
    {
        public static int SimularityIndex(string original, string query)
        {
            if (original == null || query == null)
                return 0;
            if (original == query)
                return int.MaxValue;

            if (original.StartsWith(query))
                return 1_000_000_000 - (query.Length - original.Length);

            var matches = original.Split('/', '\\')
                .Where(part => query.Contains(part))
                .Count();

            if (original.Contains(query))
                return 1_000_000 + matches - (query.Length - original.Length);

            return matches;
        }

        public static string NormalizePath(string path)
        {
            if (path == null)
                return null;
            try
            {
                return Path.GetFullPath(new Uri(path).LocalPath)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    .ToLowerInvariant();
            }
            catch
            {
                return path.ToLowerInvariant();
            }
        }
    }
}