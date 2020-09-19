using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WatchSatData;

namespace WatcherSatData_UI.Services
{
    public class ServiceStateChangedEventArgs : EventArgs
    {
        public ServiceStateChangedEventArgs(bool available)
        {
            Available = available;
        }

        public bool Available { get; }
    }

    public interface IWatcherServiceProvider
    {
        IService GetService();

        void Init();

        event EventHandler<ServiceStateChangedEventArgs> StateChanged;
    }
}
