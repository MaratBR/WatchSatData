using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WatcherSatData_UI.Services;
using WatcherSatData_UI.ServicesImpl;
using WatcherSatData_UI.Views;

namespace WatcherSatData_UI
{
    class MainModule : IModule
    {
        private IRegionManager _regionManager;

        public MainModule(IRegionManager regionManager)
        {
            _regionManager = regionManager;
        }

        public void OnInitialized(IContainerProvider containerProvider)
        {
            _regionManager.AddToRegion("Main", containerProvider.Resolve<WatcherDirectoriesConfigView>());

            containerProvider.Resolve<IServiceDetector>().EnsureServiceAvailability().ConfigureAwait(false);
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
        }
    }
}
