using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using SeroDesk.Models;
using SeroDesk.Platform;
using SeroDesk.Services;

namespace SeroDesk.ViewModels
{
    public class DesktopViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<AppIcon> _desktopIcons;
        private AppIcon? _selectedIcon;
        private double _iconScale = 1.0;
        
        public ObservableCollection<AppIcon> DesktopIcons
        {
            get => _desktopIcons;
            set { _desktopIcons = value; OnPropertyChanged(); }
        }
        
        public AppIcon? SelectedIcon
        {
            get => _selectedIcon;
            set
            {
                if (_selectedIcon != null)
                    _selectedIcon.IsSelected = false;
                
                _selectedIcon = value;
                
                if (_selectedIcon != null)
                    _selectedIcon.IsSelected = true;
                
                OnPropertyChanged();
            }
        }
        
        public double IconScale
        {
            get => _iconScale;
            set { _iconScale = value; OnPropertyChanged(); }
        }
        
        public DesktopViewModel()
        {
            _desktopIcons = new ObservableCollection<AppIcon>();
        }
        
        public async Task LoadAllApplicationsAsync()
        {
            // Clear existing icons
            DesktopIcons.Clear();
            
            // Add test apps immediately
            var testApps = new[]
            {
                new AppIcon { Name = "Notepad", ExecutablePath = @"C:\Windows\System32\notepad.exe" },
                new AppIcon { Name = "Calculator", ExecutablePath = @"C:\Windows\System32\calc.exe" },
                new AppIcon { Name = "Paint", ExecutablePath = @"C:\Windows\System32\mspaint.exe" },
                new AppIcon { Name = "Command Prompt", ExecutablePath = @"C:\Windows\System32\cmd.exe" },
                new AppIcon { Name = "Explorer", ExecutablePath = @"C:\Windows\explorer.exe" }
            };
            
            int row = 0;
            int col = 0;
            
            foreach (var app in testApps)
            {
                if (System.IO.File.Exists(app.ExecutablePath))
                {
                    app.IconImage = IconExtractor.GetIconForFile(app.ExecutablePath, true);
                    app.GridRow = row;
                    app.GridColumn = col;
                    
                    DesktopIcons.Add(app);
                    
                    col++;
                    if (col > 3) // 4 icons per row for iOS style
                    {
                        col = 0;
                        row++;
                    }
                }
            }
            
            // Add system icons
            AddSystemIcons();
            
            // Try to scan for more applications
            try
            {
                var scannedApps = await ApplicationScanner.ScanInstalledApplicationsAsync();
                
                foreach (var app in scannedApps.Take(20)) // Limit to 20 apps
                {
                    app.GridRow = row;
                    app.GridColumn = col;
                    app.Position = new Point(col * 100, row * 120);
                    
                    DesktopIcons.Add(app);
                    
                    col++;
                    if (col > 3)
                    {
                        col = 0;
                        row++;
                    }
                }
            }
            catch { }
        }
        
        public async void LoadDesktopIcons()
        {
            // Clear any existing icons first
            DesktopIcons.Clear();
            
            // Load saved icon layout
            var savedIcons = await LoadSavedIconLayout();
            
            if (savedIcons.Count == 0)
            {
                // First run - scan for applications
                var scannedApps = await ApplicationScanner.ScanInstalledApplicationsAsync();
                
                // Add default system icons
                AddSystemIcons();
                
                // Add scanned applications (unique only)
                var addedApps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                int row = 1;
                int col = 0;
                
                foreach (var app in scannedApps.Take(40)) // Increase limit but ensure uniqueness
                {
                    if (!addedApps.Contains(app.ExecutablePath))
                    {
                        addedApps.Add(app.ExecutablePath);
                        
                        app.GridRow = row;
                        app.GridColumn = col;
                        app.Position = new Point(col * 140, row * 140); // Match new icon sizes
                        
                        DesktopIcons.Add(app);
                        
                        col++;
                        if (col > 7) // 8 icons per row
                        {
                            col = 0;
                            row++;
                        }
                    }
                }
            }
            else
            {
                // Load saved icons (ensure no duplicates)
                var addedIds = new HashSet<string>();
                foreach (var icon in savedIcons)
                {
                    if (!addedIds.Contains(icon.Id))
                    {
                        addedIds.Add(icon.Id);
                        DesktopIcons.Add(icon);
                    }
                }
            }
            
            // Save layout
            SaveIconLayout();
            
            System.Diagnostics.Debug.WriteLine($"Loaded {DesktopIcons.Count} desktop icons total");
        }
        
        private void AddSystemIcons()
        {
            // This PC
            var computerIcon = new AppIcon
            {
                Name = "This PC",
                ExecutablePath = "explorer.exe",
                Arguments = "::{20D04FE0-3AEA-1069-A2D8-08002B30309D}",
                IconImage = IconExtractor.GetSystemIcon(SystemIconType.Computer),
                Type = IconType.System,
                GridRow = 0,
                GridColumn = 0,
                Position = new Point(0, 0)
            };
            DesktopIcons.Add(computerIcon);
            
            // Recycle Bin
            var recycleBin = new AppIcon
            {
                Name = "Recycle Bin",
                ExecutablePath = "explorer.exe",
                Arguments = "::{645FF040-5081-101B-9F08-00AA002F954E}",
                IconImage = IconExtractor.GetSystemIcon(SystemIconType.RecycleBin),
                Type = IconType.System,
                GridRow = 0,
                GridColumn = 1,
                Position = new Point(100, 0)
            };
            DesktopIcons.Add(recycleBin);
        }
        
        public void SelectIcon(AppIcon icon)
        {
            SelectedIcon = icon;
        }
        
        public void MoveIcon(AppIcon icon, Point newPosition)
        {
            // Snap to grid
            int newCol = (int)(newPosition.X / 100);
            int newRow = (int)(newPosition.Y / 120);
            
            // Check if position is occupied
            var occupiedIcon = DesktopIcons.FirstOrDefault(i => 
                i != icon && i.GridColumn == newCol && i.GridRow == newRow);
            
            if (occupiedIcon != null)
            {
                // Swap positions
                occupiedIcon.GridColumn = icon.GridColumn;
                occupiedIcon.GridRow = icon.GridRow;
                occupiedIcon.Position = icon.Position;
            }
            
            icon.GridColumn = newCol;
            icon.GridRow = newRow;
            icon.Position = new Point(newCol * 100, newRow * 120);
            
            SaveIconLayout();
        }
        
        public void AddFileIcon(string filePath, Point position)
        {
            var icon = new AppIcon
            {
                Name = Path.GetFileName(filePath),
                ExecutablePath = filePath,
                IconImage = IconExtractor.GetIconForFile(filePath, true),
                Type = File.GetAttributes(filePath).HasFlag(FileAttributes.Directory) 
                    ? IconType.Folder : IconType.File
            };
            
            MoveIcon(icon, position);
            DesktopIcons.Add(icon);
            SaveIconLayout();
        }
        
        public void CreateFolder(Point position)
        {
            var folder = new IconFolder
            {
                Name = "New Folder",
                IconImage = IconExtractor.GetSystemIcon(SystemIconType.Folder)
            };
            
            MoveIcon(folder, position);
            DesktopIcons.Add(folder);
            SaveIconLayout();
        }
        
        public void DeleteIcon(AppIcon icon)
        {
            DesktopIcons.Remove(icon);
            SaveIconLayout();
        }
        
        public void SwapIcons(AppIcon icon1, AppIcon icon2)
        {
            // Swap grid positions
            var tempCol = icon1.GridColumn;
            var tempRow = icon1.GridRow;
            var tempPos = icon1.Position;
            
            icon1.GridColumn = icon2.GridColumn;
            icon1.GridRow = icon2.GridRow;
            icon1.Position = icon2.Position;
            
            icon2.GridColumn = tempCol;
            icon2.GridRow = tempRow;
            icon2.Position = tempPos;
            
            SaveIconLayout();
        }
        
        private async Task<List<AppIcon>> LoadSavedIconLayout()
        {
            var icons = new List<AppIcon>();
            
            try
            {
                var configPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "SeroDesk", "desktop_layout.json");
                
                if (File.Exists(configPath))
                {
                    var json = await File.ReadAllTextAsync(configPath);
                    var savedIcons = Newtonsoft.Json.JsonConvert.DeserializeObject<List<SavedIcon>>(json);
                    
                    if (savedIcons != null)
                    {
                        foreach (var saved in savedIcons)
                        {
                            var icon = new AppIcon
                            {
                                Id = saved.Id,
                                Name = saved.Name,
                                ExecutablePath = saved.ExecutablePath,
                                Arguments = saved.Arguments,
                                WorkingDirectory = saved.WorkingDirectory,
                                GridRow = saved.GridRow,
                                GridColumn = saved.GridColumn,
                                Position = new Point(saved.GridColumn * 100, saved.GridRow * 120),
                                Type = saved.Type,
                                IsPinned = saved.IsPinned
                            };
                            
                            // Load icon image
                            if (icon.Type == IconType.System)
                            {
                                icon.IconImage = IconExtractor.GetSystemIcon(
                                    Enum.Parse<SystemIconType>(saved.SystemIconType ?? "Folder"));
                            }
                            else
                            {
                                icon.IconImage = IconExtractor.GetIconForFile(icon.ExecutablePath, true);
                            }
                            
                            icons.Add(icon);
                        }
                    }
                }
            }
            catch { }
            
            return icons;
        }
        
        private async void SaveIconLayout()
        {
            try
            {
                var configDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "SeroDesk");
                
                Directory.CreateDirectory(configDir);
                
                var savedIcons = DesktopIcons.Select(icon => new SavedIcon
                {
                    Id = icon.Id,
                    Name = icon.Name,
                    ExecutablePath = icon.ExecutablePath,
                    Arguments = icon.Arguments,
                    WorkingDirectory = icon.WorkingDirectory,
                    GridRow = icon.GridRow,
                    GridColumn = icon.GridColumn,
                    Type = icon.Type,
                    IsPinned = icon.IsPinned,
                    SystemIconType = icon.Type == IconType.System ? 
                        GetSystemIconType(icon.Name) : null
                }).ToList();
                
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(savedIcons, 
                    Newtonsoft.Json.Formatting.Indented);
                
                await File.WriteAllTextAsync(Path.Combine(configDir, "desktop_layout.json"), json);
            }
            catch { }
        }
        
        private string? GetSystemIconType(string name)
        {
            return name switch
            {
                "This PC" => "Computer",
                "Recycle Bin" => "RecycleBin",
                "Network" => "Network",
                _ => null
            };
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        private class SavedIcon
        {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string ExecutablePath { get; set; } = string.Empty;
            public string Arguments { get; set; } = string.Empty;
            public string WorkingDirectory { get; set; } = string.Empty;
            public int GridRow { get; set; }
            public int GridColumn { get; set; }
            public IconType Type { get; set; }
            public bool IsPinned { get; set; }
            public string? SystemIconType { get; set; }
        }
    }
}