using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using SeroDesk.Models;
using SeroDesk.Platform;

namespace SeroDesk.ViewModels
{
    public class DockViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<WindowInfo> _runningApplications;
        private List<string> _pinnedApps;
        private string _pinnedAppsConfigPath;
        private bool _showRecentApps = true;
        
        public ObservableCollection<WindowInfo> RunningApplications
        {
            get => _runningApplications;
            set { _runningApplications = value; OnPropertyChanged(); }
        }
        
        public bool ShowRecentApps
        {
            get => _showRecentApps;
            set 
            { 
                _showRecentApps = value; 
                OnPropertyChanged();
                // Update the applications list when this changes
                UpdateRunningApplications();
            }
        }
        
        public DockViewModel()
        {
            _runningApplications = new ObservableCollection<WindowInfo>();
            _pinnedApps = new List<string>();
            
            _pinnedAppsConfigPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SeroDesk", "pinned_apps.json");
                
            LoadPinnedApps();
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
            
            // Check if app is pinned
            bool isPinned = false;
            try
            {
                var process = System.Diagnostics.Process.GetProcessById((int)window.ProcessId);
                var appPath = process.MainModule?.FileName;
                
                if (!string.IsNullOrEmpty(appPath) && _pinnedApps.Contains(appPath))
                {
                    isPinned = true;
                    return true; // Always show pinned apps
                }
            }
            catch { }
            
            // Don't show system dialogs unless pinned
            var systemTitles = new[] { "Task Manager", "Control Panel", "Settings" };
            if (systemTitles.Any(title => window.Title.Contains(title)))
                return isPinned; // Only show if pinned
            
            // Show other running applications only if ShowRecentApps is enabled
            return _showRecentApps || isPinned;
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        // Methods for pinning/unpinning apps
        public void RemoveFromDock(WindowInfo window)
        {
            try
            {
                var process = System.Diagnostics.Process.GetProcessById((int)window.ProcessId);
                var appPath = process.MainModule?.FileName;
                
                if (!string.IsNullOrEmpty(appPath))
                {
                    _pinnedApps.Remove(appPath);
                    SavePinnedApps();
                    
                    // Update the running applications list
                    UpdateRunningApplications();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error removing from dock: {ex.Message}");
            }
        }
        
        public void AddToDock(AppIcon app)
        {
            try
            {
                var appPath = app.ExecutablePath;
                if (!string.IsNullOrEmpty(appPath) && !_pinnedApps.Contains(appPath))
                {
                    _pinnedApps.Add(appPath);
                    SavePinnedApps();
                    
                    // Update the running applications list
                    UpdateRunningApplications();
                    
                    System.Diagnostics.Debug.WriteLine($"Pinned {app.Name} to dock");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding to dock: {ex.Message}");
            }
        }
        
        public bool IsAppPinned(AppIcon app)
        {
            var appPath = app.ExecutablePath;
            return !string.IsNullOrEmpty(appPath) && _pinnedApps.Contains(appPath);
        }
        
        private void LoadPinnedApps()
        {
            try
            {
                var configDir = Path.GetDirectoryName(_pinnedAppsConfigPath);
                if (!string.IsNullOrEmpty(configDir))
                {
                    Directory.CreateDirectory(configDir);
                }
                
                if (File.Exists(_pinnedAppsConfigPath))
                {
                    var json = File.ReadAllText(_pinnedAppsConfigPath);
                    var pinnedApps = JsonConvert.DeserializeObject<List<string>>(json);
                    if (pinnedApps != null)
                    {
                        _pinnedApps = pinnedApps;
                        System.Diagnostics.Debug.WriteLine($"Loaded {_pinnedApps.Count} pinned apps");
                    }
                }
                else
                {
                    // Add some default pinned apps
                    _pinnedApps = new List<string>
                    {
                        @"C:\Windows\System32\notepad.exe",
                        @"C:\Windows\System32\calc.exe"
                    };
                    SavePinnedApps();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading pinned apps: {ex.Message}");
                _pinnedApps = new List<string>();
            }
        }
        
        private void SavePinnedApps()
        {
            try
            {
                var json = JsonConvert.SerializeObject(_pinnedApps, Formatting.Indented);
                File.WriteAllText(_pinnedAppsConfigPath, json);
                System.Diagnostics.Debug.WriteLine($"Saved {_pinnedApps.Count} pinned apps");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving pinned apps: {ex.Message}");
            }
        }
    }
}