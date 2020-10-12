using System;
using System.IO;
using System.Text;
using System.Windows;
using NLog;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using WatcherSatData_UI.Services;

namespace WatcherSatData_UI
{
    /// <summary>
    ///     Interaction logic for App.xaml
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
            var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SatDataWatcher");
            Directory.CreateDirectory(root);

            var fileTarget = new FileTarget("logfile")
            {
                AutoFlush = true,
                FileName = Path.Combine(root, "ui.log"),
                Encoding = Encoding.UTF8
            };
            fileTarget.Layout =
                new SimpleLayout("${longdate}|${level:uppercase=true}|${logger}|Thread-${threadid}|${message}");

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