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
        
        public MainViewModel()
        {
            _desktopViewModel = new DesktopViewModel();
            _launchpadViewModel = new LaunchpadViewModel();
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