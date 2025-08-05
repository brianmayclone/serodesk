using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

namespace SeroDesk.Models
{
    /// <summary>
    /// Represents an application icon with all associated metadata and UI state information.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The AppIcon class encapsulates all information needed to display and interact with
    /// an application in the SeroDesk interface. This includes:
    /// <list type="bullet">
    /// <item>Basic application information (name, path, publisher)</item>
    /// <item>Visual representation (icon image, position, scale)</item>
    /// <item>Launch parameters (arguments, working directory)</item>
    /// <item>UI state (selection, dragging, grouping)</item>
    /// <item>Layout information (grid position, grouping)</item>
    /// </list>
    /// </para>
    /// <para>
    /// The class implements INotifyPropertyChanged to support data binding in WPF,
    /// ensuring the UI automatically updates when properties change.
    /// </para>
    /// <para>
    /// AppIcon instances can exist independently or as part of an AppGroup for
    /// organized application collections.
    /// </para>
    /// </remarks>
    public class AppIcon : INotifyPropertyChanged
    {
        /// <summary>
        /// The display name of the application.
        /// </summary>
        private string _name = string.Empty;
        
        /// <summary>
        /// The full path to the application's executable file.
        /// </summary>
        private string _executablePath = string.Empty;
        
        /// <summary>
        /// Command-line arguments to pass when launching the application.
        /// </summary>
        private string _arguments = string.Empty;
        
        /// <summary>
        /// The working directory to use when launching the application.
        /// </summary>
        private string _workingDirectory = string.Empty;
        
        /// <summary>
        /// The icon image to display for this application.
        /// </summary>
        private ImageSource? _iconImage;
        
        /// <summary>
        /// The current position of the icon in canvas coordinates.
        /// </summary>
        private Point _position;
        
        /// <summary>
        /// The grid row position for layout calculations.
        /// </summary>
        private int _gridRow;
        
        /// <summary>
        /// The grid column position for layout calculations.
        /// </summary>
        private int _gridColumn;
        
        /// <summary>
        /// Indicates whether this icon is currently selected in the UI.
        /// </summary>
        private bool _isSelected;
        
        /// <summary>
        /// Indicates whether this icon is currently being dragged.
        /// </summary>
        private bool _isDragging;
        
        /// <summary>
        /// The current visual scale factor for the icon.
        /// </summary>
        private double _scale = 1.0;
        
        /// <summary>
        /// The publisher or company name of the application.
        /// </summary>
        private string _publisher = string.Empty;
        
        /// <summary>
        /// Gets or sets the unique identifier for this application icon.
        /// </summary>
        /// <value>
        /// A unique string identifier, automatically generated as a GUID for new instances.
        /// </value>
        /// <remarks>
        /// This ID is used for configuration persistence, grouping, and internal references.
        /// It should remain stable across application sessions.
        /// </remarks>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Gets or sets the display name of the application.
        /// </summary>
        /// <value>
        /// The human-readable name shown to users in the LaunchPad interface.
        /// </value>
        /// <remarks>
        /// This is typically the application's friendly name rather than the executable filename.
        /// For example, "Microsoft Word" rather than "winword.exe".
        /// </remarks>
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }
        
        /// <summary>
        /// Gets or sets the full path to the application's executable file.
        /// </summary>
        /// <value>
        /// The complete file system path to the .exe file that launches this application.
        /// </value>
        /// <remarks>
        /// This path is used for launching the application and extracting icon information.
        /// It should be a valid, accessible file path on the local system.
        /// </remarks>
        public string ExecutablePath
        {
            get => _executablePath;
            set { _executablePath = value; OnPropertyChanged(); }
        }
        
        /// <summary>
        /// Gets or sets the command-line arguments to pass when launching the application.
        /// </summary>
        /// <value>
        /// A string containing command-line arguments, or empty if no arguments are needed.
        /// </value>
        /// <remarks>
        /// These arguments are passed to the application when it's launched through SeroDesk.
        /// Common examples include file paths to open or startup options.
        /// </remarks>
        public string Arguments
        {
            get => _arguments;
            set { _arguments = value; OnPropertyChanged(); }
        }
        
        /// <summary>
        /// Gets or sets the working directory to use when launching the application.
        /// </summary>
        /// <value>
        /// The directory path to set as the current working directory, or empty to use the default.
        /// </value>
        /// <remarks>
        /// Some applications require a specific working directory to function correctly.
        /// If empty, the system will use the application's installation directory as the default.
        /// </remarks>
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