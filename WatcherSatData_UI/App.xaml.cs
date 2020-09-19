using CommonServiceLocator;
using Prism;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using Prism.Unity;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using WatcherSatData_UI.Services;
using WatcherSatData_UI.ServicesImpl;
using WatchSatData;


namespace WatcherSatData_UI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            Exit += App_Exit;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Container.Resolve<IWatcherServiceProvider>().Init();
        }

        private void App_Exit(object sender, ExitEventArgs e)
        {
            var detector = Container.Resolve<IServiceDetector>();
            if (detector.IsEmbed())
            {
                detector.StopEmbed();
            }
        }

        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        {
            base.ConfigureModuleCatalog(moduleCatalog);

            moduleCatalog.AddModule<MainModule>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton(typeof(IServiceDetector), typeof(ServiceDetector));
            containerRegistry.RegisterSingleton(typeof(IWatcherServiceProvider), typeof(WatcherServiceProvider));
        }

        protected override void ConfigureRegionAdapterMappings(RegionAdapterMappings regionAdapterMappings)
        {
            base.ConfigureRegionAdapterMappings(regionAdapterMappings);
        }
    }
}
