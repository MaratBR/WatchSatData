using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using WatcherSatData_UI.Services;
using WatcherSatData_UI.Utils.Proc;

namespace WatcherSatData_UI.ServicesImpl
{
    class ServiceDetector : IServiceDetector
    {
        private IWatcherServiceProvider _provider;
        private Supervisor serviceSupervisor;
        private static string[] ServiceExe = { "satWatcher.exe", "WatcherSatData_CLI.exe" };

        public ServiceDetector(IWatcherServiceProvider provider)
        {
            _provider = provider;
        }

        public async Task<bool> IsRunning()
        {
            try
            {
                await _provider.GetService().Ping();
                return true;
            }
            catch (EndpointNotFoundException)
            {
                return false;
            }
        }

        public void TryRun()
        {
            var exe = GetServiceExeFileOr();

            if (exe != null && serviceSupervisor == null)
            {
                try
                {
                    CreateEmbedProcess();
                }
                catch (Exception exc)
                {
                    throw exc;
                }
            }
        }


        private static string GetServiceExeFileOr()
        {
            foreach (var exe in ServiceExe)
            {
                var fullPath = GetFullPathOrNull(exe);
                if (fullPath != null)
                    return fullPath;
            }
            return null;
        }

        public static string GetFullPathOrNull(string fileName)
        {
            if (File.Exists(fileName))
                return fileName;

            var values = Environment.GetEnvironmentVariable("PATH");
            foreach (var path in values.Split(Path.PathSeparator))
            {
                var fullPath = Path.Combine(path, fileName);
                if (File.Exists(fullPath))
                    return fullPath;
            }

            var root = Directory.GetParent(System.Reflection.Assembly.GetEntryAssembly().Location).FullName;
            var exe = Path.Combine(root, fileName);
            if (File.Exists(exe))
                return exe;

            return null;
        }

        public bool IsEmbed() => serviceSupervisor != null;

        public void StopEmbed()
        {
            serviceSupervisor?.Dispose();
            serviceSupervisor = null;
        }

        private void CreateEmbedProcess()
        {
            if (serviceSupervisor != null)
            {
                StopEmbed();
            }

            var exe = GetServiceExeFileOr();
            var parent = Process.GetCurrentProcess();

            serviceSupervisor = new Supervisor(new ProcessStartInfo
            {
                FileName = exe,
                Arguments = $"--parent-pid {parent.Id}",
                CreateNoWindow = true
            });
        }
    }
}
