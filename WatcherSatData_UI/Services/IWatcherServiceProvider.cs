using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WatchSatData;

namespace WatcherSatData_UI.Services
{
    public enum ServiceState
    { 
        Offline,
        Online,
        ExpectingOnline
    }

    public class ServiceStateChangedEventArgs : EventArgs
    {
        public ServiceStateChangedEventArgs(bool available)
        {
            State = available ? ServiceState.Online : ServiceState.Offline;
        }

        public bool Available => State == ServiceState.Online;

        public ServiceState State { get; }
    }

    public interface IWatcherServiceProvider : IDisposable
    {
        ServiceState? GetLastState();

        IService GetService();

        Task InitAsync();

        bool IsEmbed();

        event EventHandler<ServiceStateChangedEventArgs> StateChanged;
    }
}
