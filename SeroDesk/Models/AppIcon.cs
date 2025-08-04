using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

namespace SeroDesk.Models
{
    public class AppIcon : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private string _executablePath = string.Empty;
        private string _arguments = string.Empty;
        private string _workingDirectory = string.Empty;
        private ImageSource? _iconImage;
        private Point _position;
        private int _gridRow;
        private int _gridColumn;
        private bool _isSelected;
        private bool _isDragging;
        private double _scale = 1.0;
        private string _publisher = string.Empty;
        
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }
        
        public string ExecutablePath
        {
            get => _executablePath;
            set { _executablePath = value; OnPropertyChanged(); }
        }
        
        public string Arguments
        {
            get => _arguments;
            set { _arguments = value; OnPropertyChanged(); }
        }
        
        public string WorkingDirectory
        {
            get => _workingDirectory;
            set { _workingDirectory = value; OnPropertyChanged(); }
        }
        
        public string Publisher
        {
            get => _publisher;
            set { _publisher = value; OnPropertyChanged(); }
        }
        
        public ImageSource? IconImage
        {
            get => _iconImage;
            set { _iconImage = value; OnPropertyChanged(); }
        }
        
        public Point Position
        {
            get => _position;
            set { _position = value; OnPropertyChanged(); }
        }
        
        public int GridRow
        {
            get => _gridRow;
            set { _gridRow = value; OnPropertyChanged(); }
        }
        
        public int GridColumn
        {
            get => _gridColumn;
            set { _gridColumn = value; OnPropertyChanged(); }
        }
        
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }
        
        public bool IsDragging
        {
            get => _isDragging;
            set { _isDragging = value; OnPropertyChanged(); }
        }
        
        public double Scale
        {
            get => _scale;
            set { _scale = value; OnPropertyChanged(); }
        }
        
        public IconType Type { get; set; } = IconType.Application;
        
        public DateTime LastAccessed { get; set; } = DateTime.Now;
        
        public int LaunchCount { get; set; }
        
        public bool IsPinned { get; set; }
        
        public string? FolderId { get; set; }
        
        public string? GroupId { get; set; }
        
        public int GroupIndex { get; set; }
        
        public void Launch()
        {
            try
            {
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = ExecutablePath,
                    Arguments = Arguments,
                    WorkingDirectory = string.IsNullOrEmpty(WorkingDirectory) 
                        ? System.IO.Path.GetDirectoryName(ExecutablePath) ?? string.Empty 
                        : WorkingDirectory,
                    UseShellExecute = true,
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal
                };
                
                var process = System.Diagnostics.Process.Start(startInfo);
                
                if (process != null)
                {
                    // Run window activation in background task to avoid blocking UI
                    Task.Run(() => BringProcessToForeground(process));
                }
                
                LaunchCount++;
                LastAccessed = DateTime.Now;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Failed to launch {Name}: {ex.Message}",
                    "Launch Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        
        private void BringProcessToForeground(System.Diagnostics.Process process)
        {
            try
            {
                // Wait for process to initialize (up to 3 seconds)
                for (int i = 0; i < 30; i++)
                {
                    if (process.HasExited)
                        return;
                        
                    // Refresh to get latest MainWindowHandle
                    process.Refresh();
                    
                    if (process.MainWindowHandle != IntPtr.Zero)
                    {
                        // Found main window, bring it to foreground
                        Platform.NativeMethods.ShowWindow(process.MainWindowHandle, Platform.NativeMethods.SW_RESTORE);
                        Platform.NativeMethods.SetForegroundWindow(process.MainWindowHandle);
                        Platform.NativeMethods.BringWindowToTop(process.MainWindowHandle);
                        return;
                    }
                    
                    // Wait 100ms before next check
                    System.Threading.Thread.Sleep(100);
                }
                
                // If still no main window, try to find by process name
                var processes = System.Diagnostics.Process.GetProcessesByName(process.ProcessName);
                foreach (var proc in processes)
                {
                    if (proc.Id == process.Id && proc.MainWindowHandle != IntPtr.Zero)
                    {
                        Platform.NativeMethods.ShowWindow(proc.MainWindowHandle, Platform.NativeMethods.SW_RESTORE);
                        Platform.NativeMethods.SetForegroundWindow(proc.MainWindowHandle);
                        Platform.NativeMethods.BringWindowToTop(proc.MainWindowHandle);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error bringing process to foreground: {ex.Message}");
            }
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    
    public enum IconType
    {
        Application,
        Folder,
        File,
        System,
        Widget
    }
    
    public class IconFolder : AppIcon
    {
        private bool _isOpen;
        
        public ObservableCollection<AppIcon> Icons { get; } = new();
        
        public bool IsOpen
        {
            get => _isOpen;
            set { _isOpen = value; OnPropertyChanged(); }
        }
        
        public IconFolder()
        {
            Type = IconType.Folder;
            Name = "New Folder";
        }
        
        public void AddIcon(AppIcon icon)
        {
            icon.FolderId = Id;
            Icons.Add(icon);
        }
        
        public void RemoveIcon(AppIcon icon)
        {
            icon.FolderId = null;
            Icons.Remove(icon);
        }
    }
}