using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WatcherSatData_UI.ViewModels;
using WatchSatData;

namespace WatcherSatData_UI.Services
{

    public interface IWatcherServiceProvider : IDisposable
    {
        ServiceState GetLastState();

        IService GetService();

        Task InitAsync();

        bool IsEmbed();

        event EventHandler<ServiceStateChangedEventArgs> StateChanged;
    }

    public static class WatcherServiceProviderExtensions
    {
        public static void SubscribeToServiceState(this IWatcherServiceProvider provider, IServiceStateListener listener)
        {
            provider.StateChanged += listener.OnServiceStateChanged;
            listener.OnServiceStateChanged(provider, new ServiceStateChangedEventArgs(provider.GetLastState()));
        }

        public static void UnsubscribeFromServiceState(this IWatcherServiceProvider provider, IServiceStateListener listener)
        {
            provider.StateChanged += listener.OnServiceStateChanged;
            listener.OnServiceStateChanged(provider, new ServiceStateChangedEventArgs(provider.GetLastState()));
        }
    }
}
