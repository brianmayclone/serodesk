using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Newtonsoft.Json;
using SeroDesk.Models;
using SeroDesk.Platform;

namespace SeroDesk.ViewModels
{
    public class DockViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<DockItem> _dockItems;
        private List<string> _pinnedApps;
        private string _pinnedAppsConfigPath;
        private bool _showRecentApps = true;
        
        public ObservableCollection<DockItem> DockItems
        {
            get => _dockItems;
            set { _dockItems = value; OnPropertyChanged(); }
        }
        
        public bool ShowRecentApps
        {
            get => _showRecentApps;
            set 
            { 
                _showRecentApps = value; 
                OnPropertyChanged();
                // Update the applications list when this changes
                UpdateDockItems();
            }
        }
        
        public DockViewModel()
        {
            _dockItems = new ObservableCollection<DockItem>();
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
                    UpdateDockItems();
                });
            };
            
            // Initial population
            UpdateDockItems();
        }
        
        private void UpdateDockItems()
        {
            var runningWindows = WindowManager.Instance.Windows
                .Where(ShouldShowAsRunningWindow)
                .ToList();

            var windowsByPath = new Dictionary<string, WindowInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var window in runningWindows)
            {
                var appPath = TryGetProcessPath(window.ProcessId);
                if (!string.IsNullOrEmpty(appPath) && !windowsByPath.ContainsKey(appPath))
                {
                    window.PropertyChanged -= Window_PropertyChanged;
                    window.PropertyChanged += Window_PropertyChanged;
                    windowsByPath[appPath] = window;
                }
            }

            DockItems.Clear();

            foreach (var pinnedPath in _pinnedApps.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                DockItems.Add(CreateDockItem(pinnedPath, windowsByPath.TryGetValue(pinnedPath, out var runningWindow) ? runningWindow : null, isPinned: true));
            }

            foreach (var pair in windowsByPath)
            {
                if (_pinnedApps.Contains(pair.Key, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                DockItems.Add(CreateDockItem(pair.Key, pair.Value, isPinned: false));
            }

            OnPropertyChanged(nameof(DockItems));
        }
        
        private void Window_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Update UI when window properties change
            OnPropertyChanged(nameof(DockItems));
        }
        
        private bool ShouldShowAsRunningWindow(WindowInfo window)
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
                var appPath = TryGetProcessPath(window.ProcessId);
                
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
        public void RemoveFromDock(DockItem dockItem)
        {
            try
            {
                var appPath = dockItem.ExecutablePath;
                
                if (!string.IsNullOrEmpty(appPath))
                {
                    _pinnedApps.Remove(appPath);
                    SavePinnedApps();
                    
                    UpdateDockItems();
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
                    
                    UpdateDockItems();
                    
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

        private DockItem CreateDockItem(string executablePath, WindowInfo? runningWindow, bool isPinned)
        {
            return new DockItem
            {
                DisplayName = GetDisplayName(executablePath, runningWindow),
                ExecutablePath = executablePath,
                IconImage = GetIconImage(executablePath, runningWindow),
                IsPinned = isPinned,
                WindowInfo = runningWindow
            };
        }

        private string GetDisplayName(string executablePath, WindowInfo? runningWindow)
        {
            if (!string.IsNullOrWhiteSpace(runningWindow?.Title))
            {
                return runningWindow.Title;
            }

            try
            {
                if (File.Exists(executablePath))
                {
                    var versionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(executablePath);
                    if (!string.IsNullOrWhiteSpace(versionInfo.ProductName))
                    {
                        return versionInfo.ProductName;
                    }
                }
            }
            catch { }

            return Path.GetFileNameWithoutExtension(executablePath);
        }

        private ImageSource? GetIconImage(string executablePath, WindowInfo? runningWindow)
        {
            try
            {
                if (runningWindow?.Icon != null)
                {
                    return Imaging.CreateBitmapSourceFromHIcon(
                        runningWindow.Icon.Handle,
                        Int32Rect.Empty,
                        System.Windows.Media.Imaging.BitmapSizeOptions.FromWidthAndHeight(64, 64));
                }
            }
            catch { }

            return IconExtractor.GetIconForFile(executablePath, true);
        }

        private string? TryGetProcessPath(uint processId)
        {
            try
            {
                var process = System.Diagnostics.Process.GetProcessById((int)processId);
                return process.MainModule?.FileName;
            }
            catch
            {
                return null;
            }
        }
    }
}
