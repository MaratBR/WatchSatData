using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WatcherSatData_UI.ViewModels;

namespace WatcherSatData_UI.Views
{
    /// <summary>
    /// Interaction logic for WatcherDirectoriesConfigView.xaml
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
            var vm = (DirectoriesConfigViewModel)DataContext;
            await vm.RefreshData();
        }
    }
}
