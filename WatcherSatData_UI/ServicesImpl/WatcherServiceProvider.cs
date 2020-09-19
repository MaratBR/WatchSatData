using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WatcherSatData_UI.Services;
using WatchSatData;

namespace WatcherSatData_UI.ServicesImpl
{
    class WatcherServiceProvider : IWatcherServiceProvider
    {
        public event EventHandler<ServiceStateChangedEventArgs> StateChanged;

        private IService service;
        private Task pingerTask;
        private bool pingLoopRunning = true;

        public WatcherServiceProvider()
        {
        }

        public IService GetService()
        {
            if (service == null)
            {
                service = new WatcherServiceProxy(this, 5);
                ((WatcherServiceProxy)service).StateChanged += WatcherServiceProvider_StateChanged;
            }
            return service;
        }

        public void Init()
        {
            pingerTask = PingLoop();
        }

        private async Task PingLoop()
        {
            while (pingLoopRunning)
            {
                await Task.Delay(10000);
                await GetService().Ping();
            }
        }

        private void WatcherServiceProvider_StateChanged(object sender, ServiceStateChangedEventArgs e)
        {
            StateChanged?.Invoke(this, e);
        }

        public IService InstantiateService()
        {
            try
            {
                var binding = new NetNamedPipeBinding()
                {
                    Security = new NetNamedPipeSecurity { Mode = NetNamedPipeSecurityMode.Transport }
                };
                EndpointAddress endpoint = new EndpointAddress("net.pipe://localhost/birdsWatcher_30c58e1c-300d-4dfb-ae9b-01da83d5c7d6/v1");
                var channelFactory = new ChannelFactory<IService>(binding, endpoint);
                return channelFactory.CreateChannel();
            }
            catch
            {
                return null;
            }
        }
    }
}
