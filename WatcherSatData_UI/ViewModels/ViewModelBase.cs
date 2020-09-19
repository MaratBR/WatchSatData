using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WatcherSatData_UI.ViewModels
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void Set<T>(ref T valueRef, T newValue, [CallerMemberName] string propertyName = null)
        {
            valueRef = newValue;
            OnPropertyChanged(propertyName);
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}