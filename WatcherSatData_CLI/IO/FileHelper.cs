using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel.Description;
using System.Text;
using System.Threading.Tasks;

namespace WatcherSatData_CLI.IO
{
    static class FileHelper
    {
        public static async Task<FileStream> OpenFileWithAttemts(string path, FileMode mode, int n)
        {
            int attempts = 0;
            FileStream fs = null;
            Exception exc = null;
            
            while (attempts++ <= n && fs == null)
            {
                try
                {
                    fs = File.Open(path, mode, FileAccess.ReadWrite, FileShare.ReadWrite);
                }
                catch (Exception _exc)
                {
                    exc = _exc;
                    await Task.Delay(TimeSpan.FromSeconds(5));
                }
            }

            if (fs == null)
                throw exc;
            else
                return fs;
        }
    }
}
