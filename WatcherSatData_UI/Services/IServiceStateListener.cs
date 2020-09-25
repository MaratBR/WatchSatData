using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WatcherSatData_UI.Services
{
    public interface IServiceStateListener
    {
        void OnServiceStateChanged(object sender, ServiceStateChangedEventArgs e);
    }
}
