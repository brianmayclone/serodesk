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
        // Desktop functionality is DISABLED - only LaunchPad is used
        // private DesktopViewModel _desktopViewModel; // REMOVED
        private LaunchpadViewModel _launchpadViewModel;
        private bool _isDesktopVisible = true;
        private ObservableCollection<AppIcon> _runningApplications;
        private ObservableCollection<object> _notifications;
        private double _brightnessLevel = 75;
        private double _volumeLevel = 50;
        
        /// <summary>
        /// Desktop functionality is disabled - LaunchPad handles all applications
        /// </summary>
        [Obsolete("Desktop functionality is disabled. Use Launchpad instead.")]
        public ObservableCollection<AppIcon> DesktopIcons => new ObservableCollection<AppIcon>();
        
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
            // Desktop functionality is DISABLED - only LaunchPad is used
            // _desktopViewModel = new DesktopViewModel(); // REMOVED
            _launchpadViewModel = new LaunchpadViewModel();
            _runningApplications = new ObservableCollection<AppIcon>();
            _notifications = new ObservableCollection<object>();
        }
        
        /// <summary>
        /// Desktop icons are not used in this app - LaunchPad handles all apps
        /// </summary>
        [Obsolete("Desktop functionality is disabled. Use Launchpad.LoadAllApplicationsAsync() instead.")]
        public void LoadDesktopIcons()
        {
            // Desktop icons are not used in this app - LaunchPad handles all apps
            // Method kept for compatibility but does nothing
            System.Diagnostics.Debug.WriteLine("LoadDesktopIcons called but Desktop functionality is disabled");
        }
        
        public async void LoadAllAppsForSpringBoard()
        {
            // Only load for Launchpad - Desktop icons are not used in this app
            // await Desktop.LoadAllApplicationsAsync(); // DISABLED
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