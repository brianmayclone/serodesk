using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace SeroDesk.Platform
{
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
    
    public class WindowManager : IDisposable
    {
        private static WindowManager? _instance;
        private readonly ObservableCollection<WindowInfo> _windows;
        private readonly System.Timers.Timer _updateTimer;
        private bool _disposed = false;
        
        public static WindowManager Instance => _instance ?? (_instance = new WindowManager());
        
        public ObservableCollection<WindowInfo> Windows => _windows;
        
        private WindowManager()
        {
            _windows = new ObservableCollection<WindowInfo>();
            
            _updateTimer = new System.Timers.Timer(1000); // Update every second
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
                    return System.Drawing.Icon.ExtractAssociatedIcon(process.MainModule.FileName);
                }
            }
            catch { }
            
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
                var props = new DWM_THUMBNAIL_PROPERTIES
                {
                    dwFlags = 0x1F, // All properties
                    opacity = 255,
                    fVisible = true,
                    fSourceClientAreaOnly = true,
                    rcDestination = new RECT
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