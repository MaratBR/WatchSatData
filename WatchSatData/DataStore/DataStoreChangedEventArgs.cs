using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WatchSatData.DataStore
{
    public class DataStoreChangedEventArgs : EventArgs
    {
        public enum Change
        {
            Refresh,
            DirectoryCreated,
            DirectoryDeleted,
            DirectoryUpdated
        }

        public Change Type { get; set; }

        public DirectoryCleanupConfig Record { get; set; }
    }
}
