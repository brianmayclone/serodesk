using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using SeroDesk.Constants;

namespace SeroDesk.Platform
{
    /// <summary>
    /// Represents information about a window in the system for management and display purposes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// WindowInfo encapsulates all relevant data about a system window that SeroDesk
    /// needs for window management, taskbar display, and user interaction.
    /// </para>
    /// <para>
    /// The class implements INotifyPropertyChanged to support data binding in UI scenarios
    /// where window information is displayed in lists or controls.
    /// </para>
    /// </remarks>
    public class WindowInfo : INotifyPropertyChanged
    {
        private string _title = string.Empty;
        private IntPtr _handle;
        private uint _processId;
        private bool _isMinimized;
        private bool _isRunning = true;
        private System.Drawing.Icon? _icon;
        
        public IntPtr Handle
        {
            get => _handle;
            set { _handle = value; OnPropertyChanged(); }
        }
        
        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(); }
        }
        
        public uint ProcessId
        {
            get => _processId;
            set { _processId = value; OnPropertyChanged(); }
        }
        
        public bool IsMinimized
        {
            get => _isMinimized;
            set { _isMinimized = value; OnPropertyChanged(); }
        }
        
        public bool IsRunning
        {
            get => _isRunning;
            set { _isRunning = value; OnPropertyChanged(); }
        }
        
        public System.Drawing.Icon? Icon
        {
            get => _icon;
            set { _icon = value; OnPropertyChanged(); }
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    
    /// <summary>
    /// Provides comprehensive window management functionality for the SeroDesk shell environment.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The WindowManager class serves as the central component for interacting with system windows.
    /// It provides:
    /// <list type="bullet">
    /// <item>Enumeration and tracking of all visible system windows</item>
    /// <item>Window thumbnails and previews using Desktop Window Manager (DWM)</item>
    /// <item>Window state management (minimize, restore, bring to front)</item>
    /// <item>Icon extraction for both traditional Win32 and UWP applications</item>
    /// <item>Automatic refresh of window list for dynamic updates</item>
    /// <item>Integration with taskbar and window switching functionality</item>
    /// </list>
    /// </para>
    /// <para>
    /// The class uses a singleton pattern to ensure consistent window state across the application
    /// and implements IDisposable for proper cleanup of system resources and timers.
    /// </para>
    /// <para>
    /// Window information is automatically refreshed at regular intervals defined by
    /// <see cref="UIConstants.WindowUpdateIntervalMs"/> to keep the UI synchronized with
    /// the actual system state.
    /// </para>
    /// </remarks>
    public class WindowManager : IDisposable
    {
        /// <summary>
        /// Singleton instance of the WindowManager.
        /// </summary>
        private static WindowManager? _instance;
        
        /// <summary>
        /// Observable collection of all tracked windows for UI binding.
        /// </summary>
        private readonly ObservableCollection<WindowInfo> _windows;
        
        /// <summary>
        /// Timer that periodically refreshes the window list to maintain accuracy.
        /// </summary>
        private readonly System.Timers.Timer _updateTimer;
        
        /// <summary>
        /// Indicates whether this instance has been disposed.
        /// </summary>
        private bool _disposed = false;
        
        /// <summary>
        /// Gets the singleton instance of the WindowManager.
        /// </summary>
        /// <value>The global WindowManager instance.</value>
        /// <remarks>
        /// The instance is created on first access and maintained throughout the application lifetime.
        /// </remarks>
        public static WindowManager Instance => _instance ?? (_instance = new WindowManager());
        
        /// <summary>
        /// Gets the observable collection of all tracked windows.
        /// </summary>
        /// <value>A collection of WindowInfo objects representing visible system windows.</value>
        /// <remarks>
        /// This collection is automatically updated and can be bound to UI elements for
        /// displaying window lists, taskbars, or switchers.
        /// </remarks>
        public ObservableCollection<WindowInfo> Windows => _windows;
        
        private WindowManager()
        {
            _windows = new ObservableCollection<WindowInfo>();
            
            _updateTimer = new System.Timers.Timer(UIConstants.WindowUpdateIntervalMs);
            _updateTimer.Elapsed += (s, e) => RefreshWindowList();
            _updateTimer.Start();
            
            RefreshWindowList();
        }
        
        public void RefreshWindowList()
        {
            if (_disposed) return;
            
            try
            {
                var currentWindows = new List<WindowInfo>();
                
                NativeMethods.EnumWindows((hWnd, lParam) =>
                {
                    if (IsWindowValid(hWnd))
                    {
                        var windowInfo = GetWindowInfo(hWnd);
                        if (windowInfo != null)
                        {
                            currentWindows.Add(windowInfo);
                        }
                    }
                    return true;
                }, IntPtr.Zero);
                
                // Update collection on UI thread safely
                var dispatcher = System.Windows.Application.Current?.Dispatcher;
                if (dispatcher != null && !dispatcher.HasShutdownStarted && !_disposed)
                {
                    if (dispatcher.CheckAccess())
                    {
                        UpdateWindowCollection(currentWindows);
                    }
                    else
                    {
                        dispatcher.BeginInvoke(new Action(() => UpdateWindowCollection(currentWindows)));
                    }
                }
            }
            catch (TaskCanceledException)
            {
                // Ignore task cancellation during shutdown
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing window list: {ex.Message}");
            }
        }
        
        private bool IsWindowValid(IntPtr hWnd)
        {
            // Check if window is visible
            if (!NativeMethods.IsWindowVisible(hWnd))
                return false;
            
            // Get window text length
            int length = NativeMethods.GetWindowTextLength(hWnd);
            if (length == 0)
                return false;
            
            // Check window style
            int style = NativeMethods.GetWindowLong(hWnd, NativeMethods.GWL_STYLE);
            int exStyle = NativeMethods.GetWindowLong(hWnd, NativeMethods.GWL_EXSTYLE);
            
            // Filter out tool windows and other special windows
            if ((exStyle & NativeMethods.WS_EX_TOOLWINDOW) != 0)
                return false;
            
            return true;
        }
        
        private WindowInfo? GetWindowInfo(IntPtr hWnd)
        {
            var info = new WindowInfo { Handle = hWnd };
            
            // Get window title
            var sb = new StringBuilder(256);
            NativeMethods.GetWindowText(hWnd, sb, sb.Capacity);
            info.Title = sb.ToString();
            
            // Get process ID
            NativeMethods.GetWindowThreadProcessId(hWnd, out uint processId);
            info.ProcessId = processId;
            
            // Get window state
            var placement = new WINDOWPLACEMENT();
            GetWindowPlacement(hWnd, ref placement);
            info.IsMinimized = placement.showCmd == NativeMethods.SW_MINIMIZE;
            
            // Get window icon
            info.Icon = GetWindowIcon(hWnd, processId);
            
            return info;
        }
        
        private System.Drawing.Icon? GetWindowIcon(IntPtr hWnd, uint processId)
        {
            try
            {
                // Try to get the window icon
                IntPtr hIcon = SendMessage(hWnd, WM_GETICON, ICON_BIG, IntPtr.Zero);
                if (hIcon == IntPtr.Zero)
                    hIcon = SendMessage(hWnd, WM_GETICON, ICON_SMALL, IntPtr.Zero);
                if (hIcon == IntPtr.Zero)
                    hIcon = GetClassLongPtr(hWnd, GCL_HICON);
                if (hIcon == IntPtr.Zero)
                    hIcon = GetClassLongPtr(hWnd, GCL_HICONSM);
                
                if (hIcon != IntPtr.Zero)
                {
                    return System.Drawing.Icon.FromHandle(hIcon);
                }
                
                // If no icon found, try to get from process executable
                var process = System.Diagnostics.Process.GetProcessById((int)processId);
                if (process.MainModule != null)
                {
                    var fileName = process.MainModule.FileName;
                    
                    // Special handling for UWP apps
                    if (IsUWPApp(process))
                    {
                        return GetUWPAppIcon(process);
                    }
                    
                    return System.Drawing.Icon.ExtractAssociatedIcon(fileName);
                }
            }
            catch { }
            
            return null;
        }
        
        private bool IsUWPApp(System.Diagnostics.Process process)
        {
            try
            {
                var fileName = process.MainModule?.FileName;
                if (fileName == null) return false;
                
                // UWP apps typically run from WindowsApps folder
                if (fileName.Contains("WindowsApps")) return true;
                
                // Additional UWP detection methods
                if (fileName.Contains("Program Files\\WindowsApps")) return true;
                if (fileName.Contains("Microsoft.WindowsStore")) return true;
                if (fileName.Contains("Microsoft.Office")) return true;
                
                // Check if process has UWP characteristics
                try
                {
                    var processName = process.ProcessName.ToLower();
                    if (processName == "microsoftstore" || 
                        processName == "winstore.app" ||
                        processName.Contains("uwp") ||
                        processName.Contains("appx"))
                    {
                        return true;
                    }
                }
                catch { }
                
                return false;
            }
            catch
            {
                return false;
            }
        }
        
        private System.Drawing.Icon? GetUWPAppIcon(System.Diagnostics.Process process)
        {
            try
            {
                if (process.MainModule?.FileName == null) return null;
                
                var fileName = process.MainModule.FileName;
                System.Diagnostics.Debug.WriteLine($"Loading UWP icon for: {fileName}");
                
                // Try to get the package name from the executable path
                if (fileName.Contains("WindowsApps"))
                {
                    // Extract package info from path like: C:\Program Files\WindowsApps\Microsoft.WindowsStore_...
                    var pathParts = fileName.Split('\\');
                    for (int i = 0; i < pathParts.Length; i++)
                    {
                        if (pathParts[i] == "WindowsApps" && i + 1 < pathParts.Length)
                        {
                            var packageFolder = pathParts[i + 1];
                            System.Diagnostics.Debug.WriteLine($"Package folder: {packageFolder}");
                            
                            // Try to find the app manifest and extract icon
                            var packagePath = string.Join("\\", pathParts, 0, i + 2);
                            var manifestPath = System.IO.Path.Combine(packagePath, "AppxManifest.xml");
                            
                            if (System.IO.File.Exists(manifestPath))
                            {
                                System.Diagnostics.Debug.WriteLine($"Found manifest: {manifestPath}");
                                var icon = ExtractIconFromUWPManifest(packagePath, manifestPath);
                                if (icon != null) return icon;
                            }
                            break;
                        }
                    }
                }
                
                // Fallback 1: Try SHGetFileInfo with larger icon
                var iconSource = IconExtractor.GetIconForFile(fileName, true);
                if (iconSource != null)
                {
                    System.Diagnostics.Debug.WriteLine($"UWP icon loaded with SHGetFileInfo");
                    // Convert ImageSource to Icon - this is tricky, let's try a different approach
                    return System.Drawing.Icon.ExtractAssociatedIcon(fileName);
                }
                
                // Fallback 2: Try direct extraction
                return System.Drawing.Icon.ExtractAssociatedIcon(fileName);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UWP icon extraction failed: {ex.Message}");
            }
            
            return null;
        }
        
        private System.Drawing.Icon? ExtractIconFromUWPManifest(string packagePath, string manifestPath)
        {
            try
            {
                // This is a simplified approach - a full implementation would parse the XML
                // and extract the correct icon based on scale and size
                
                // Look for common icon files in the package
                var commonIconNames = new[] { "StoreLogo.png", "Square44x44Logo.png", "Square150x150Logo.png", "LargeTile.png", "SmallTile.png" };
                
                foreach (var iconName in commonIconNames)
                {
                    var iconPath = System.IO.Path.Combine(packagePath, iconName);
                    if (System.IO.File.Exists(iconPath))
                    {
                        System.Diagnostics.Debug.WriteLine($"Found UWP icon file: {iconPath}");
                        
                        // Convert PNG to Icon
                        using (var bitmap = new System.Drawing.Bitmap(iconPath))
                        {
                            var hIcon = bitmap.GetHicon();
                            return System.Drawing.Icon.FromHandle(hIcon);
                        }
                    }
                }
                
                // Also check Assets folder
                var assetsPath = System.IO.Path.Combine(packagePath, "Assets");
                if (System.IO.Directory.Exists(assetsPath))
                {
                    var pngFiles = System.IO.Directory.GetFiles(assetsPath, "*.png");
                    foreach (var pngFile in pngFiles)
                    {
                        var fileName = System.IO.Path.GetFileName(pngFile).ToLower();
                        if (fileName.Contains("logo") || fileName.Contains("icon"))
                        {
                            System.Diagnostics.Debug.WriteLine($"Found UWP asset icon: {pngFile}");
                            
                            using (var bitmap = new System.Drawing.Bitmap(pngFile))
                            {
                                var hIcon = bitmap.GetHicon();
                                return System.Drawing.Icon.FromHandle(hIcon);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UWP manifest icon extraction failed: {ex.Message}");
            }
            
            return null;
        }
        
        private void UpdateWindowCollection(List<WindowInfo> currentWindows)
        {
            // Remove windows that no longer exist
            for (int i = _windows.Count - 1; i >= 0; i--)
            {
                if (!currentWindows.Any(w => w.Handle == _windows[i].Handle))
                {
                    _windows.RemoveAt(i);
                }
            }
            
            // Add new windows or update existing ones
            foreach (var window in currentWindows)
            {
                var existing = _windows.FirstOrDefault(w => w.Handle == window.Handle);
                if (existing != null)
                {
                    // Update existing window
                    existing.Title = window.Title;
                    existing.IsMinimized = window.IsMinimized;
                    if (window.Icon != null)
                        existing.Icon = window.Icon;
                }
                else
                {
                    // Add new window
                    _windows.Add(window);
                }
            }
        }
        
        public void MinimizeAllWindows()
        {
            foreach (var window in _windows)
            {
                if (!window.IsMinimized)
                {
                    NativeMethods.ShowWindow(window.Handle, NativeMethods.SW_MINIMIZE);
                }
            }
        }
        
        public void RestoreWindow(IntPtr hWnd)
        {
            NativeMethods.ShowWindow(hWnd, NativeMethods.SW_RESTORE);
            NativeMethods.SetForegroundWindow(hWnd);
        }
        
        public void ShowWindowSwitcher()
        {
            // This will be implemented as a visual window switcher UI
            // For now, just cycle through windows
            if (_windows.Count > 0)
            {
                var currentForeground = NativeMethods.GetForegroundWindow();
                var currentIndex = _windows.ToList().FindIndex(w => w.Handle == currentForeground);
                var nextIndex = (currentIndex + 1) % _windows.Count;
                
                RestoreWindow(_windows[nextIndex].Handle);
            }
        }
        
        public IntPtr CreateWindowThumbnail(IntPtr targetWindow, IntPtr hostWindow, 
            System.Windows.Rect bounds)
        {
            IntPtr thumbnail;
            int result = NativeMethods.DwmRegisterThumbnail(hostWindow, targetWindow, out thumbnail);
            
            if (result == 0 && thumbnail != IntPtr.Zero)
            {
                var props = new NativeMethods.DWM_THUMBNAIL_PROPERTIES
                {
                    dwFlags = 0x1F, // All properties
                    opacity = 255,
                    fVisible = true,
                    fSourceClientAreaOnly = true,
                    rcDestination = new NativeMethods.RECT
                    {
                        Left = (int)bounds.Left,
                        Top = (int)bounds.Top,
                        Right = (int)bounds.Right,
                        Bottom = (int)bounds.Bottom
                    }
                };
                
                NativeMethods.DwmUpdateThumbnailProperties(thumbnail, ref props);
                return thumbnail;
            }
            
            return IntPtr.Zero;
        }
        
        public void DestroyWindowThumbnail(IntPtr thumbnail)
        {
            if (thumbnail != IntPtr.Zero)
            {
                NativeMethods.DwmUnregisterThumbnail(thumbnail);
            }
        }
        
        #region Additional P/Invoke
        
        private const int WM_GETICON = 0x7F;
        private const int ICON_SMALL = 0;
        private const int ICON_BIG = 1;
        private const int GCL_HICON = -14;
        private const int GCL_HICONSM = -34;
        
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, IntPtr lParam);
        
        [DllImport("user32.dll")]
        private static extern IntPtr GetClassLongPtr(IntPtr hWnd, int nIndex);
        
        [DllImport("user32.dll")]
        private static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);
        
        [StructLayout(LayoutKind.Sequential)]
        private struct WINDOWPLACEMENT
        {
            public int length;
            public int flags;
            public int showCmd;
            public System.Drawing.Point ptMinPosition;
            public System.Drawing.Point ptMaxPosition;
            public System.Drawing.Rectangle rcNormalPosition;
        }
        
        #endregion
        
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _updateTimer?.Stop();
                _updateTimer?.Dispose();
            }
        }
    }
}