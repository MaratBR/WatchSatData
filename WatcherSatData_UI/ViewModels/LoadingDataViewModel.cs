using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Prism.Commands;
using Prism.Regions;

namespace WatcherSatData_UI.ViewModels
{
    public abstract class LoadingDataViewModel<TData> : ViewModelBase, INavigationAware
    {
        private TData data;

        private bool isLoading;
        private Exception lastException;

        public LoadingDataViewModel()
        {
            Refresh = new DelegateCommand(async () => await RefreshData(), CanRefresh);
        }

        public TData Data
        {
            get => data;
            private set => Set(ref data, value);
        }

        public ICommand Refresh { get; }

        public bool IsLoading
        {
            get => isLoading;
            private set => Set(ref isLoading, value);
        }

        public Exception LastException
        {
            get => lastException;
            private set => Set(ref lastException, value);
        }

        public async void OnNavigatedTo(NavigationContext navigationContext)
        {
            await RefreshData();
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return false;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
        }

        protected abstract Task<TData> LoadData();

        protected bool CanRefresh()
        {
            return true;
        }

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
    }
}