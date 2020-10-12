using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Prism.Commands;
using WatcherSatData_UI.Services;
using WatcherSatData_UI.Utils;
using WatchSatData.DataStore;

namespace WatcherSatData_UI.ViewModels
{
    public class DirectoriesConfigViewModel : LoadingDataViewModel<IEnumerable<DirectoryConfigViewModel>>, IDisposable,
        IServiceStateListener
    {
        private readonly DebounceDispatcher debounceDispatcher = new DebounceDispatcher();

        private readonly IWatcherServiceProvider _serviceProvider;

        private string queryString;
        private ServiceState serviceState;

        public DirectoriesConfigViewModel(
            IWatcherServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
                throw new ArgumentNullException(nameof(serviceProvider));
            _serviceProvider = serviceProvider;
            serviceProvider.SubscribeToServiceState(this);

            Save = new DelegateCommand(async () => await SaveImpl());
            Add = new DelegateCommand(AddNewImpl);
        }

        public ICommand Save { get; }
        public ICommand Add { get; }

        public string QueryString
        {
            get => queryString;
            set
            {
                Set(ref queryString, value);
                if (string.IsNullOrWhiteSpace(value))
                    RefreshData().ConfigureAwait(false);
                else
                    debounceDispatcher.Debounce(250, _s => RefreshData().ConfigureAwait(false));
            }
        }

        public ServiceState ServiceState
        {
            get => serviceState;
            set
            {
                Set(ref serviceState, value);
                OnPropertyChanged(nameof(ServiceAvailable));
            }
        }

        public bool ServiceAvailable => ServiceState != ServiceState.Offline;

        public void Dispose()
        {
            _serviceProvider.UnsubscribeFromServiceState(this);
        }

        public async void OnServiceStateChanged(object sender, ServiceStateChangedEventArgs e)
        {
            if (e.Available)
                await RefreshData();

            ServiceState = e.State;
        }

        private async Task SaveImpl()
        {
            try
            {
                if (Data != null)
                {
                    await Task.WhenAll(Data.Select(x => x.SaveChanges()));
                    await RefreshData();
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void AddNewImpl()
        {
            if (Data is IList<DirectoryConfigViewModel> l)
            {
                var vm = new DirectoryConfigViewModel(_serviceProvider.GetService());
                vm.RequestRemove += delegate { l.Remove(vm); };
                l.Add(vm);
            }
        }

        protected override async Task<IEnumerable<DirectoryConfigViewModel>> LoadData()
        {
            IEnumerable<DirectoryCleanupConfig> config;
            var service = _serviceProvider.GetService();
            if (!string.IsNullOrWhiteSpace(queryString))
                config = await service.FindDirectoriesByPath(queryString);
            else
                config = await service.GetAllDirectories();
            var vms = config.Select(x =>
            {
                var vm = new DirectoryConfigViewModel(service);
                vm.OriginalConfig = x;
                return vm;
            }).ToList();
            return new ObservableCollection<DirectoryConfigViewModel>(vms);
        }
    }
}