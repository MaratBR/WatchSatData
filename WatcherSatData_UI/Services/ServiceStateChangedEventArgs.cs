using System;

namespace WatcherSatData_UI.Services
{
    public class ServiceStateChangedEventArgs : EventArgs
    {
        public ServiceStateChangedEventArgs(bool available)
        {
            State = available ? ServiceState.Online : ServiceState.Offline;
        }

        public ServiceStateChangedEventArgs(ServiceState serviceState)
        {
            State = serviceState;
        }

        public bool Available => State == ServiceState.Online;

        public ServiceState State { get; }
    }
}