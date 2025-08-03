using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using SeroDesk.Models;
using SeroDesk.Services;

namespace SeroDesk.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private DesktopViewModel _desktopViewModel;
        private LaunchpadViewModel _launchpadViewModel;
        private bool _isDesktopVisible = true;
        private ObservableCollection<AppIcon> _runningApplications;
        private ObservableCollection<object> _notifications;
        private double _brightnessLevel = 75;
        private double _volumeLevel = 50;
        
        public DesktopViewModel Desktop
        {
            get => _desktopViewModel;
            set { _desktopViewModel = value; OnPropertyChanged(); }
        }
        
        public LaunchpadViewModel Launchpad
        {
            get => _launchpadViewModel;
            set { _launchpadViewModel = value; OnPropertyChanged(); }
        }
        
        public bool IsDesktopVisible
        {
            get => _isDesktopVisible;
            set { _isDesktopVisible = value; OnPropertyChanged(); }
        }
        
        public ObservableCollection<AppIcon> DesktopIcons => Desktop.DesktopIcons;
        
        public ObservableCollection<AppIcon> RunningApplications
        {
            get => _runningApplications;
            set { _runningApplications = value; OnPropertyChanged(); }
        }
        
        public ObservableCollection<object> Notifications
        {
            get => _notifications;
            set { _notifications = value; OnPropertyChanged(); }
        }
        
        public bool HasNotifications => Notifications?.Count > 0;
        
        public double BrightnessLevel
        {
            get => _brightnessLevel;
            set { _brightnessLevel = value; OnPropertyChanged(); }
        }
        
        public double VolumeLevel
        {
            get => _volumeLevel;
            set { _volumeLevel = value; OnPropertyChanged(); }
        }
        
        public MainViewModel()
        {
            _desktopViewModel = new DesktopViewModel();
            _launchpadViewModel = new LaunchpadViewModel();
            _runningApplications = new ObservableCollection<AppIcon>();
            _notifications = new ObservableCollection<object>();
        }
        
        public void LoadDesktopIcons()
        {
            Desktop.LoadDesktopIcons();
        }
        
        public async void LoadAllAppsForSpringBoard()
        {
            await Desktop.LoadAllApplicationsAsync();
            // Also load for Launchpad
            await Launchpad.LoadAllApplicationsAsync();
        }
        
        public void LoadWidgets(Canvas widgetContainer)
        {
            // Initialize Widget Manager with container
            WidgetManager.Instance.Initialize(widgetContainer);
        }
        
        public void ToggleDesktop()
        {
            IsDesktopVisible = !IsDesktopVisible;
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}