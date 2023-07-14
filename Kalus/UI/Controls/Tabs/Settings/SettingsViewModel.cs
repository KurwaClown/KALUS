using System.ComponentModel;

namespace Kalus.UI.Controls.Tabs.Settings
{
    internal class SettingsViewModel : INotifyPropertyChanged
    {

        private bool runOnStartUp;
        private bool openWithClient;

        public bool RunOnStartUp
        {
            get { return runOnStartUp; }
            set
            {
                runOnStartUp = value;
                OnPropertyChanged(nameof(RunOnStartUp));
            }
        }

        public bool OpenWithClient
        {
            get { return openWithClient; }
            set
            {
                openWithClient = value;
                OnPropertyChanged(nameof(OpenWithClient));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
