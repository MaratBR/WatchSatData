using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WatcherSatData_CLI.WatcherImpl
{
    public class JsonDataStoreOptions : AbstractFileDataStoreOptions
    {
        public bool Pretty { get; set; } = false;
    }
}
