using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WatcherSatData_UI.Utils.Proc
{
    class SupervisorStateChangedEventArgs : EventArgs
    {
        public bool IsAlive { get; set; }
    }
}
