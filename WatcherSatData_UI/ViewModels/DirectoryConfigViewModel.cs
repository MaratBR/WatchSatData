using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using WatchSatData;
using WatchSatData.DataStore;
using WatchSatData.Watcher;

namespace WatcherSatData_UI.ViewModels
{
    public class DirectoryConfigViewModel : ViewModelBase
    {
        private IService service;

        private string alias;
        private string fullPath;
        private string filter;
        private double maxAge;
        private CleanupTarget target;
        private bool? exists;
        private DateTime? lastCleanup;
        private DateTime addedAt;

        private bool isChanged = false;
        private bool isDeleted = false;
        private DirectoryCleanupConfig config;

        public event EventHandler RequestRemove;
        public ICommand ToggleDeleted { get; }
        public ICommand Reset { get; }
        public ICommand Save { get; }

        public string Alias { get => alias; set { Set(ref alias, value); OnChanged(); } }

        public string FullPath { get => fullPath; set { Set(ref fullPath, value); OnChanged(); } }

        public string Filter { get => filter; set { Set(ref filter, value); OnChanged(); } }

        public CleanupTarget Target { get => target; set { Set(ref target, value); OnChanged(); } }

        public double MaxAge { get => maxAge; set { Set(ref maxAge, value); OnChanged(); } }

        public DateTime? LastCleanup
        {
            get => lastCleanup;
            private set => Set(ref lastCleanup, value);
        }

        public bool? Exists { get => exists; private set => Set(ref exists, value); }

        public DateTime AddedAt
        {
            get => addedAt;
            private set => Set(ref addedAt, value);
        }

        public bool IsChanged
        {
            get => isChanged;
            private set => Set(ref isChanged, value);
        }

        public bool IsDeleted
        {
            get => isDeleted;
            set
            {
                Set(ref isDeleted, value);
                OnChanged();
            }
        }

        public bool IsNew
        {
            get => config == null;
        }

        public DirectoryCleanupConfig OriginalConfig
        {
            get => config;
            set
            {
                Set(ref config, value);

                OnNewConfig();
            }
        }

        private void OnChanged()
        {
            IsChanged = HasChanges();
        }

        private void OnNewConfig()
        {
            Alias = config.Alias;
            FullPath = config.FullPath;
            Exists = config.Exists;
            MaxAge = config.MaxAge;
            AddedAt = config.AddedAt;
            LastCleanup = config.LastCleanupTime;
            Target = config.CleanupTarget;
            Filter = config.Filter;
            IsChanged = false;
            OnPropertyChanged(nameof(IsNew));
        }

        private bool HasChanges()
        {
            if (config == null || IsDeleted)
                return true;

            return config.Alias != alias?.Trim() ||
                config.CleanupTarget != target ||
                PathUtils.NormalizePath(config.FullPath) != PathUtils.NormalizePath(fullPath) ||
                Math.Abs(maxAge - config.MaxAge) > 1e-5 ||
                filter != config.Filter;
        }

        private void ToggleDeletedImpl()
        {
            if (IsNew)
            {
                RequestRemove?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                IsDeleted = !IsDeleted;
                OnChanged();
            }
        }
        
        private void ResetImpl()
        {
            OnNewConfig();
        }

        public Task SaveChanges()
        {
            if (IsDeleted)
            {
                if (IsNew)
                {
                    RequestRemove?.Invoke(this, EventArgs.Empty);
                    return Task.CompletedTask;
                }
                else
                {
                    return service.DeleteDirectory(config.Id);
                }
            }
            else if (IsChanged)
            {
                var newConfig = IsNew ? new DirectoryCleanupConfig() : (DirectoryCleanupConfig)config.Clone();

                newConfig.Alias = alias?.Trim();
                newConfig.Alias = newConfig.Alias == string.Empty ? null : newConfig.Alias;
                newConfig.FullPath = PathUtils.NormalizePath(fullPath);
                newConfig.MaxAge = maxAge;
                newConfig.Filter = filter;
                newConfig.CleanupTarget = target;

                if (IsNew)
                {
                    return service.CreateDirectory(newConfig);
                }
                else
                {                    
                    return service.UpdateDirectory(newConfig);
                }
            }
            return Task.CompletedTask;
        }

        public DirectoryConfigViewModel(IService service)
        {
            AddedAt = DateTime.Now;
            this.service = service;

            ToggleDeleted = new DelegateCommand(ToggleDeletedImpl);
            Reset = new DelegateCommand(ResetImpl);
            Save = new DelegateCommand(async () => await SaveChanges());
        }
    }
}
