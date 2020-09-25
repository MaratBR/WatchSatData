using CommonServiceLocator;
using NLog;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;
using Prism;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using Prism.Unity;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Unity;
using Unity.Lifetime;
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
            InitLog();

            base.OnStartup(e);
        }

        private void InitLog()
        {
            var config = new LoggingConfiguration();
            var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WatchSatData");
            Directory.CreateDirectory(root);

            var fileTarget = new FileTarget("logfile")
            {
                AutoFlush = true,
                FileName = Path.Combine(root, "ui.log"),
                Encoding = Encoding.UTF8
            };
            fileTarget.Layout = new SimpleLayout("${longdate}|${level:uppercase=true}|${logger}|Thread-${threadid}|${message}");

            config.AddTarget(fileTarget);
            config.AddRule(LogLevel.Trace, LogLevel.Off, fileTarget);

            LogManager.Configuration = config;
        }

        private void App_Exit(object sender, ExitEventArgs e)
        {
            var provider = Container.Resolve<IWatcherServiceProvider>();
            provider.Dispose();
        }

        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        {
            base.ConfigureModuleCatalog(moduleCatalog);

            moduleCatalog.AddModule<MainModule>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
        }

        protected override void ConfigureRegionAdapterMappings(RegionAdapterMappings regionAdapterMappings)
        {
            base.ConfigureRegionAdapterMappings(regionAdapterMappings);
        }
    }
}
