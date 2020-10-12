using System.Windows;
using System.Windows.Controls;
using WatcherSatData_UI.ViewModels;

namespace WatcherSatData_UI.Views
{
    /// <summary>
    ///     Interaction logic for WatcherDirectoriesConfigView.xaml
    /// </summary>
    public partial class WatcherDirectoriesConfigView : UserControl
    {
        public WatcherDirectoriesConfigView(DirectoriesConfigViewModel viewModel)
        {
            InitializeComponent();

            Loaded += WatcherDirectoriesConfigView_Loaded;
            DataContext = viewModel;
        }

        private async void WatcherDirectoriesConfigView_Loaded(object sender, RoutedEventArgs e)
        {
            var vm = (DirectoriesConfigViewModel) DataContext;
            await vm.RefreshData();
        }
    }
}