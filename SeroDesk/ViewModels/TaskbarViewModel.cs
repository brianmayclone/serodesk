using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using SeroDesk.Models;
using SeroDesk.Platform;
using SeroDesk.Services;

namespace SeroDesk.ViewModels
{
    public class TaskbarViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<AppIcon> _quickLaunchApps;
        private ObservableCollection<WindowInfo> _runningApplications;
        
        public ObservableCollection<AppIcon> QuickLaunchApps
        {
            get => _quickLaunchApps;
            set { _quickLaunchApps = value; OnPropertyChanged(); }
        }
        
        public ObservableCollection<WindowInfo> RunningApplications
        {
            get => _runningApplications;
            set { _runningApplications = value; OnPropertyChanged(); }
        }
        
        public TaskbarViewModel()
        {
            _quickLaunchApps = new ObservableCollection<AppIcon>();
            _runningApplications = new ObservableCollection<WindowInfo>();
        }
        
        public async void LoadQuickLaunchApps()
        {
            // Load saved quick launch apps
            var savedApps = await LoadSavedQuickLaunchApps();
            
            if (savedApps.Count == 0)
            {
                // Add default apps
                AddDefaultQuickLaunchApps();
            }
            else
            {
                foreach (var app in savedApps)
                {
                    QuickLaunchApps.Add(app);
                }
            }
        }
        
        private void AddDefaultQuickLaunchApps()
        {
            // File Explorer
            var explorer = new AppIcon
            {
                Name = "File Explorer",
                ExecutablePath = "explorer.exe",
                IconImage = IconExtractor.GetSystemIcon(SystemIconType.Folder)
            };
            QuickLaunchApps.Add(explorer);
            
            // Edge Browser
            var edgePath = @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe";
            if (System.IO.File.Exists(edgePath))
            {
                var edge = new AppIcon
                {
                    Name = "Microsoft Edge",
                    ExecutablePath = edgePath,
                    IconImage = IconExtractor.GetIconForFile(edgePath, true)
                };
                QuickLaunchApps.Add(edge);
            }
            
            // Settings
            var settings = new AppIcon
            {
                Name = "Settings",
                ExecutablePath = "ms-settings:",
                IconImage = IconExtractor.GetIconForFile(
                    System.IO.Path.Combine(Environment.SystemDirectory, "control.exe"), true)
            };
            QuickLaunchApps.Add(settings);
            
            SaveQuickLaunchApps();
        }
        
        public void StartMonitoringWindows()
        {
            // Subscribe to WindowManager updates
            WindowManager.Instance.Windows.CollectionChanged += (s, e) =>
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    RunningApplications.Clear();
                    foreach (var window in WindowManager.Instance.Windows)
                    {
                        RunningApplications.Add(window);
                    }
                });
            };
            
            // Initial population
            foreach (var window in WindowManager.Instance.Windows)
            {
                RunningApplications.Add(window);
            }
        }
        
        public void AddToQuickLaunch(AppIcon app)
        {
            if (!QuickLaunchApps.Any(a => a.ExecutablePath == app.ExecutablePath))
            {
                QuickLaunchApps.Add(app);
                SaveQuickLaunchApps();
            }
        }
        
        public void RemoveFromQuickLaunch(AppIcon app)
        {
            QuickLaunchApps.Remove(app);
            SaveQuickLaunchApps();
        }
        
        private async Task<List<AppIcon>> LoadSavedQuickLaunchApps()
        {
            var apps = new List<AppIcon>();
            
            try
            {
                var configPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "SeroDesk", "quicklaunch.json");
                
                if (File.Exists(configPath))
                {
                    var json = await File.ReadAllTextAsync(configPath);
                    var savedApps = Newtonsoft.Json.JsonConvert.DeserializeObject<List<SavedQuickLaunchApp>>(json);
                    
                    if (savedApps != null)
                    {
                        foreach (var saved in savedApps)
                        {
                            var app = new AppIcon
                            {
                                Name = saved.Name,
                                ExecutablePath = saved.ExecutablePath,
                                Arguments = saved.Arguments,
                                WorkingDirectory = saved.WorkingDirectory
                            };
                            
                            // Load icon
                            if (saved.IsSystemIcon && !string.IsNullOrEmpty(saved.SystemIconType))
                            {
                                app.IconImage = IconExtractor.GetSystemIcon(
                                    Enum.Parse<SystemIconType>(saved.SystemIconType));
                            }
                            else
                            {
                                app.IconImage = IconExtractor.GetIconForFile(app.ExecutablePath, true);
                            }
                            
                            apps.Add(app);
                        }
                    }
                }
            }
            catch { }
            
            return apps;
        }
        
        private async void SaveQuickLaunchApps()
        {
            try
            {
                var configDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "SeroDesk");
                
                Directory.CreateDirectory(configDir);
                
                var savedApps = QuickLaunchApps.Select(app => new SavedQuickLaunchApp
                {
                    Name = app.Name,
                    ExecutablePath = app.ExecutablePath,
                    Arguments = app.Arguments,
                    WorkingDirectory = app.WorkingDirectory,
                    IsSystemIcon = app.Type == IconType.System,
                    SystemIconType = app.Type == IconType.System ? 
                        GetSystemIconType(app.Name) : null
                }).ToList();
                
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(savedApps, 
                    Newtonsoft.Json.Formatting.Indented);
                
                await File.WriteAllTextAsync(Path.Combine(configDir, "quicklaunch.json"), json);
            }
            catch { }
        }
        
        private string? GetSystemIconType(string name)
        {
            return name switch
            {
                "File Explorer" => "Folder",
                "This PC" => "Computer",
                "Network" => "Network",
                _ => null
            };
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        private class SavedQuickLaunchApp
        {
            public string Name { get; set; } = string.Empty;
            public string ExecutablePath { get; set; } = string.Empty;
            public string Arguments { get; set; } = string.Empty;
            public string WorkingDirectory { get; set; } = string.Empty;
            public bool IsSystemIcon { get; set; }
            public string? SystemIconType { get; set; }
        }
    }
}