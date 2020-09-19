using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WatchSatData.DataStore;

namespace WatcherSatData_CLI.WatcherImpl
{
    public class JsonDataStore : AbstractFileDataStore
    {
        private JsonDataStoreOptions options;
        public JsonDataStore(string filePath, JsonDataStoreOptions options) : base(filePath, options)
        {
            this.options = options;
        }

        protected override List<DirectoryCleanupConfig> ConvertFromString(string data)
        {
            return JsonConvert.DeserializeObject<List<DirectoryCleanupConfig>>(data);
        }

        protected override string ConvertToString(List<DirectoryCleanupConfig> data)
        {
            return JsonConvert.SerializeObject(data, options.Pretty ? Formatting.Indented : Formatting.None);
        }
    }
}
