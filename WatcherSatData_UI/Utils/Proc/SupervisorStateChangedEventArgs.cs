using System;

namespace WatcherSatData_UI.Utils.Proc
{
    internal class SupervisorStateChangedEventArgs : EventArgs
    {
        public bool IsAlive { get; set; }
    }
}