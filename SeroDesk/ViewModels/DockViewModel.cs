using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SeroDesk.Platform;

namespace SeroDesk.ViewModels
{
    public class DockViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<WindowInfo> _runningApplications;
        
        public ObservableCollection<WindowInfo> RunningApplications
        {
            get => _runningApplications;
            set { _runningApplications = value; OnPropertyChanged(); }
        }
        
        public DockViewModel()
        {
            _runningApplications = new ObservableCollection<WindowInfo>();
        }
        
        public void StartMonitoringWindows()
        {
            // Subscribe to WindowManager updates
            WindowManager.Instance.Windows.CollectionChanged += (s, e) =>
            {
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    UpdateRunningApplications();
                });
            };
            
            // Initial population
            UpdateRunningApplications();
        }
        
        private void UpdateRunningApplications()
        {
            RunningApplications.Clear();
            
            // Only show applications that should appear in dock
            foreach (var window in WindowManager.Instance.Windows)
            {
                if (ShouldShowInDock(window))
                {
                    // Add IsRunning property for dock indicator
                    window.PropertyChanged += Window_PropertyChanged;
                    RunningApplications.Add(window);
                }
            }
        }
        
        private void Window_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Update UI when window properties change
            OnPropertyChanged(nameof(RunningApplications));
        }
        
        private bool ShouldShowInDock(WindowInfo window)
        {
            // Filter criteria for dock visibility
            if (string.IsNullOrEmpty(window.Title))
                return false;
            
            // Don't show SeroDesk itself
            if (window.Title.Contains("SeroDesk"))
                return false;
            
            // Don't show system dialogs
            var systemTitles = new[] { "Task Manager", "Control Panel", "Settings" };
            if (systemTitles.Any(title => window.Title.Contains(title)))
                return false;
            
            return true;
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}