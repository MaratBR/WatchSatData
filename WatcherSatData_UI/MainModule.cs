using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using WatcherSatData_UI.Services;
using WatcherSatData_UI.ServicesImpl;
using WatcherSatData_UI.Views;

namespace WatcherSatData_UI
{
    internal class MainModule : IModule
    {
        private readonly IRegionManager _regionManager;

        public MainModule(IRegionManager regionManager)
        {
            _regionManager = regionManager;
        }

        public void OnInitialized(IContainerProvider containerProvider)
        {
            _regionManager.AddToRegion("Main", containerProvider.Resolve<WatcherDirectoriesConfigView>());

            containerProvider.Resolve<IWatcherServiceProvider>().InitAsync().ConfigureAwait(false);
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton(typeof(IWatcherServiceProvider), typeof(WatcherServiceProvider));
        }
    }
}