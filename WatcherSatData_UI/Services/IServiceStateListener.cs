namespace WatcherSatData_UI.Services
{
    public interface IServiceStateListener
    {
        void OnServiceStateChanged(object sender, ServiceStateChangedEventArgs e);
    }
}