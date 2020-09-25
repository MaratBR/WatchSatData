
using Prism.Commands;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace WatcherSatData_UI.ViewModels
{
    public abstract class LoadingDataViewModel<TData> : ViewModelBase, INavigationAware
    {
        private TData data;

        public TData Data
        {
            get => data;
            private set => Set(ref data, value);
        }

        public ICommand Refresh { get; }

        private bool isLoading;
        private Exception lastException;

        public bool IsLoading { get => isLoading; private set => Set(ref isLoading, value); }

        public Exception LastException { get => lastException; private set => Set(ref lastException, value); }

        public LoadingDataViewModel()
        {
            Refresh = new DelegateCommand(async () => await RefreshData(), CanRefresh);
        }

        protected abstract Task<TData> LoadData();

        protected bool CanRefresh() => true;

        public async Task RefreshData()
        {
            LastException = null;
            IsLoading = true;

            try
            {
                Data = await LoadData();
            }
            catch (Exception e)
            {
                LastException = e;
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async void OnNavigatedTo(NavigationContext navigationContext)
        {
            await RefreshData();
        }

        public bool IsNavigationTarget(NavigationContext navigationContext) => false;

        public void OnNavigatedFrom(NavigationContext navigationContext) {}
    }
}
