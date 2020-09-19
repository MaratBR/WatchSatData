using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WatcherSatData_UI.Services
{
    public interface IServiceDetector
    {
        Task<bool> IsRunning();

        void TryRun();

        bool IsEmbed();

        void StopEmbed();
    }

    public static class ServiceDetectorExtensions
    {
        public static async Task EnsureServiceAvailability(this IServiceDetector detector)
        {
            if (await detector.IsRunning())
                return;
            detector.TryRun();
            await Task.Delay(300);
        }
    }
}
