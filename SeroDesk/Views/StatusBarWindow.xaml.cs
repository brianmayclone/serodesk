using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Text;
using System.Collections.Generic;
using SeroDesk.Platform;

namespace SeroDesk.Views
{
    /// <summary>
    /// Represents the iOS-style status bar window that provides system information and quick access controls.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The StatusBarWindow implements a macOS/iOS-inspired status bar that appears at the top of the screen.
    /// Key features include:
    /// <list type="bullet">
    /// <item>Auto-hide functionality with mouse proximity detection</item>
    /// <item>Automatic window resizing to prevent overlap with other applications</item>
    /// <item>System information display (time, date, battery, network status)</item>
    /// <item>Quick access to Control Center and Notification Center</item>
    /// <item>Smooth animations for show/hide transitions</item>
    /// <item>Integration with Windows DWM for proper layering</item>
    /// </list>
    /// </para>
    /// <para>
    /// The status bar automatically detects when the mouse cursor approaches the top edge of the screen
    /// and slides down to become visible. When not in use, it auto-hides to maximize screen real estate.
    /// </para>
    /// <para>
    /// Window management integration ensures that when the status bar is visible, other application
    /// windows are automatically resized to prevent content from being hidden behind the bar.
    /// </para>
    /// </remarks>
    public partial class StatusBarWindow : Window
    {
        /// <summary>
        /// Timer for tracking mouse position to trigger auto-show functionality.
        /// </summary>
        private DispatcherTimer _mouseTrackingTimer;
        
        /// <summary>
        /// Timer for auto-hiding the status bar after a period of inactivity.
        /// </summary>
        private DispatcherTimer _autoHideTimer;
        
        /// <summary>
        /// Indicates whether the status bar is currently visible to the user.
        /// </summary>
        private bool _isVisible = true;
        
        /// <summary>
        /// Indicates whether a show/hide animation is currently in progress.
        /// </summary>
        private bool _isAnimating = false;
        
        /// <summary>
        /// Tracks whether the user is currently on the desktop or LaunchPad view.
        /// </summary>
        private bool _isOnDesktop = true;
        
        /// <summary>
        /// The height of the status bar when fully visible.
        /// </summary>
        private const int STATUS_BAR_HEIGHT = 32;
        
        /// <summary>
        /// The size of the mouse activation area at the top of the screen in pixels.
        /// </summary>
        private const int MOUSE_ACTIVATION_AREA = 5;
        
        /// <summary>
        /// Dictionary tracking windows that have been resized to accommodate the status bar.
        /// </summary>
        private Dictionary<IntPtr, WindowInfo> _resizedWindows = new Dictionary<IntPtr, WindowInfo>();
        
        /// <summary>
        /// Structure to store original window position and size information for restoration.
        /// </summary>
        /// <remarks>
        /// This structure maintains the original window state before it was modified to accommodate
        /// the status bar, enabling accurate restoration when the status bar is hidden.
        /// </remarks>
        private struct WindowInfo
        {
            /// <summary>The original X position of the window.</summary>
            public int X;
            /// <summary>The original Y position of the window.</summary>
            public int Y;
            /// <summary>The original width of the window.</summary>
            public int Width;
            /// <summary>The original height of the window.</summary>
            public int Height;
            /// <summary>Whether the window was maximized before being resized.</summary>
            public bool WasMaximized;
            
            /// <summary>
            /// Initializes a new WindowInfo structure with the specified values.
            /// </summary>
            /// <param name="x">The X position of the window.</param>
            /// <param name="y">The Y position of the window.</param>
            /// <param name="width">The width of the window.</param>
            /// <param name="height">The height of the window.</param>
            /// <param name="wasMaximized">Whether the window was maximized.</param>
            public WindowInfo(int x, int y, int width, int height, bool wasMaximized = false)
            {
                X = x; Y = y; Width = width; Height = height; WasMaximized = wasMaximized;
            }
        }
        
        public StatusBarWindow()
        {
            InitializeComponent();
            
            Loaded += StatusBarWindow_Loaded;
            
            // Initialize timers
            _mouseTrackingTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50) // Check mouse position every 50ms
            };
            _mouseTrackingTimer.Tick += MouseTrackingTimer_Tick;
            
            _autoHideTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5) // Auto-hide after 5 seconds
            };
            _autoHideTimer.Tick += AutoHideTimer_Tick;
        }
        
        private void StatusBarWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Position status bar at top of screen
            PositionStatusBarAtTop();
            
            // Connect to status bar events (if the status bar has them)
            if (StatusBar != null)
            {
                // Forward status bar events to main window or handle them here
                // StatusBar.LeftSideClicked += (s, args) => ShowNotificationCenter();
                // StatusBar.RightSideClicked += (s, args) => ShowControlCenter();
            }
            
            // Check initial state before showing
            var isDesktopActive = IsDesktopOrLaunchpadActive();
            _isOnDesktop = isDesktopActive;
            
            if (isDesktopActive)
            {
                // Start visible if desktop is active
                ShowStatusBar();
                SetStatusBarBackground(true); // Transparent for desktop
            }
            else
            {
                // Start hidden if other applications are in foreground
                _isVisible = false;
                this.Visibility = Visibility.Hidden;
            }
            
            // Start mouse tracking
            _mouseTrackingTimer.Start();
        }
        
        private void PositionStatusBarAtTop()
        {
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            
            // Position at top of screen
            this.Width = screenWidth;
            this.Height = STATUS_BAR_HEIGHT;
            this.Left = 0;
            this.Top = 0;
        }
        
        private void MouseTrackingTimer_Tick(object? sender, EventArgs e)
        {
            if (_isAnimating) return;
            
            var mousePosition = GetCursorPosition();
            var isMouseAtTop = mousePosition.Y <= MOUSE_ACTIVATION_AREA;
            var isDesktopActive = IsDesktopOrLaunchpadActive();
            
            _isOnDesktop = isDesktopActive;
            
            if (isDesktopActive)
            {
                // On desktop/launchpad: Always show status bar with transparent background
                if (!_isVisible)
                {
                    ShowStatusBar();
                }
                // Set transparent background for desktop
                SetStatusBarBackground(true);
                // Stop auto-hide timer on desktop
                _autoHideTimer.Stop();
            }
            else
            {
                // Over other applications
                if (isMouseAtTop)
                {
                    // Mouse at top: Show status bar with opaque background and reset auto-hide timer
                    if (!_isVisible)
                    {
                        ShowStatusBar();
                    }
                    // Set opaque background for overlay
                    SetStatusBarBackground(false);
                    _autoHideTimer.Stop();
                    _autoHideTimer.Start();
                }
                else if (_isVisible)
                {
                    // Mouse not at top: Start auto-hide timer if not already running
                    if (!_autoHideTimer.IsEnabled)
                    {
                        _autoHideTimer.Start();
                    }
                }
            }
        }
        
        private void AutoHideTimer_Tick(object? sender, EventArgs e)
        {
            // Only auto-hide when not on desktop and mouse is not at top
            var mousePosition = GetCursorPosition();
            var isMouseAtTop = mousePosition.Y <= MOUSE_ACTIVATION_AREA;
            
            if (!_isOnDesktop && !isMouseAtTop && _isVisible && !_isAnimating)
            {
                HideStatusBar();
            }
            
            _autoHideTimer.Stop();
        }
        
        private bool IsDesktopOrLaunchpadActive()
        {
            try
            {
                var foregroundWindow = NativeMethods.GetForegroundWindow();
                if (foregroundWindow == IntPtr.Zero) return false;
                
                // Get the class name of the foreground window
                var className = new System.Text.StringBuilder(256);
                NativeMethods.GetClassName(foregroundWindow, className, className.Capacity);
                var classNameStr = className.ToString();
                
                // Get window title to help identify SeroDesk windows
                var titleLength = NativeMethods.GetWindowTextLength(foregroundWindow);
                if (titleLength > 0)
                {
                    var title = new System.Text.StringBuilder(titleLength + 1);
                    NativeMethods.GetWindowText(foregroundWindow, title, title.Capacity);
                    var titleStr = title.ToString();
                    
                    // Check if it's any SeroDesk window (MainWindow, LaunchpadWindow, etc.)
                    if (titleStr.Contains("SeroDesk") || titleStr.Contains("Launchpad"))
                    {
                        return true;
                    }
                }
                
                // Also check class name patterns for SeroDesk or Windows desktop
                return classNameStr.Contains("SeroDesk") ||
                       classNameStr == "Progman" || // Windows desktop
                       classNameStr == "WorkerW";   // Windows desktop worker
            }
            catch
            {
                return false;
            }
        }
        
        private void ShowStatusBar()
        {
            if (_isVisible || _isAnimating) return;
            
            System.Diagnostics.Debug.WriteLine("ShowStatusBar: Starting to show StatusBar and resize windows");
            
            _isAnimating = true;
            _isVisible = true;
            
            // Ensure window is topmost
            this.Topmost = true;
            this.Visibility = Visibility.Visible;
            
            // Animate slide down from top
            var slideDown = new DoubleAnimation
            {
                From = -STATUS_BAR_HEIGHT,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            
            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(300)
            };
            
            slideDown.Completed += (s, e) => 
            {
                _isAnimating = false;
                System.Diagnostics.Debug.WriteLine("ShowStatusBar: Animation completed, calling ResizeOverlappingWindows");
                // Resize overlapping windows when StatusBar becomes visible
                ResizeOverlappingWindows();
            };
            
            var translateTransform = new System.Windows.Media.TranslateTransform();
            this.RenderTransform = translateTransform;
            
            translateTransform.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, slideDown);
            this.BeginAnimation(OpacityProperty, fadeIn);
        }
        
        private void HideStatusBar()
        {
            if (!_isVisible || _isAnimating) return;
            
            System.Diagnostics.Debug.WriteLine("HideStatusBar: Starting to hide StatusBar and restore windows");
            
            _isAnimating = true;
            _isVisible = false;
            
            // Animate slide up to top
            var slideUp = new DoubleAnimation
            {
                From = 0,
                To = -STATUS_BAR_HEIGHT,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            
            var fadeOut = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(300)
            };
            
            slideUp.Completed += (s, e) => 
            {
                _isAnimating = false;
                this.Visibility = Visibility.Hidden;
                System.Diagnostics.Debug.WriteLine("HideStatusBar: Animation completed, calling RestoreResizedWindows");
                // Restore windows to original sizes when StatusBar hides
                RestoreResizedWindows();
            };
            
            var translateTransform = this.RenderTransform as System.Windows.Media.TranslateTransform;
            if (translateTransform == null)
            {
                translateTransform = new System.Windows.Media.TranslateTransform();
                this.RenderTransform = translateTransform;
            }
            
            translateTransform.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, slideUp);
            this.BeginAnimation(OpacityProperty, fadeOut);
        }
        
        private Point GetCursorPosition()
        {
            var point = new POINT();
            GetCursorPos(out point);
            return new Point(point.X, point.Y);
        }
        
        private bool IsMouseOverStatusBar()
        {
            var mousePosition = GetCursorPosition();
            var statusBarBounds = new Rect(this.Left, this.Top, this.ActualWidth, this.ActualHeight);
            return statusBarBounds.Contains(mousePosition);
        }
        
        /// <summary>
        /// Manually show the status bar (e.g., for notifications)
        /// </summary>
        public void ForceShow()
        {
            ShowStatusBar();
            _autoHideTimer.Stop();
            _autoHideTimer.Start();
        }
        
        /// <summary>
        /// Update desktop state (call this when switching between desktop and apps)
        /// </summary>
        public void UpdateDesktopState(bool isOnDesktop)
        {
            _isOnDesktop = isOnDesktop;
            
            if (isOnDesktop && !_isVisible)
            {
                ShowStatusBar();
            }
        }
        
        /// <summary>
        /// Sets the status bar background transparency based on context
        /// </summary>
        /// <param name="isTransparent">True for transparent (desktop), false for opaque (overlay)</param>
        private void SetStatusBarBackground(bool isTransparent)
        {
            this.Dispatcher.Invoke(() =>
            {
                try
                {
                    // Get the StatusBar UserControl and call its SetBackgroundTransparency method
                    var statusBarControl = this.FindName("StatusBar") as Views.SeroStatusBar;
                    statusBarControl?.SetBackgroundTransparency(isTransparent);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to set status bar background: {ex.Message}");
                }
            });
        }
        
        private void ResizeOverlappingWindows()
        {
            System.Diagnostics.Debug.WriteLine("ResizeOverlappingWindows: Starting window resize process");
            
            // Prevent multiple simultaneous calls
            if (_resizedWindows.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine("ResizeOverlappingWindows: Already in progress, skipping");
                return;
            }
            
            // Get screen dimensions
            var screenHeight = SystemParameters.PrimaryScreenHeight;
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            
            System.Diagnostics.Debug.WriteLine($"Screen dimensions: {screenWidth}x{screenHeight}, StatusBar height: {STATUS_BAR_HEIGHT}");
            
            // Enumerate all windows
            NativeMethods.EnumWindows((hWnd, lParam) =>
            {
                try
                {
                    // Get window title first (even for invisible windows for debugging)
                    var length = NativeMethods.GetWindowTextLength(hWnd);
                    var windowTitle = "";
                    if (length > 0)
                    {
                        var title = new StringBuilder(length + 1);
                        NativeMethods.GetWindowText(hWnd, title, title.Capacity);
                        windowTitle = title.ToString();
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"EnumWindows: Found window '{windowTitle}' (Handle: {hWnd}) - Visible: {NativeMethods.IsWindowVisible(hWnd)}");
                    
                    // Check if window is visible (removed fullscreen check - resize ALL windows)
                    if (NativeMethods.IsWindowVisible(hWnd))
                    {
                        if (!string.IsNullOrEmpty(windowTitle) && 
                            !windowTitle.Contains("SeroDesk") &&
                            !windowTitle.Contains("Launchpad") &&
                            !windowTitle.Contains("Task View") &&
                            !windowTitle.Contains("Start") &&
                            windowTitle != "Program Manager")
                        {
                            // Get current window position and size
                            if (NativeMethods.GetWindowRect(hWnd, out var rect))
                            {
                                System.Diagnostics.Debug.WriteLine($"Found window '{windowTitle}' at {rect.Left},{rect.Top} {rect.Right - rect.Left}x{rect.Bottom - rect.Top}");
                                
                                // Check if window overlaps with status bar area (top 32 pixels)
                                if (rect.Top < STATUS_BAR_HEIGHT && rect.Bottom > 0)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Window '{windowTitle}' overlaps with StatusBar - will resize");
                                    bool isMaximized = IsMaximizedWindow(hWnd);
                                    
                                    // Store original position/size and maximized state
                                    _resizedWindows[hWnd] = new WindowInfo(rect.Left, rect.Top, 
                                        rect.Right - rect.Left, rect.Bottom - rect.Top, isMaximized);
                                    
                                    if (isMaximized)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"Window '{windowTitle}' is maximized - using special maximized handling");
                                        
                                        // For maximized windows, use a different approach:
                                        // Set working area to exclude StatusBar area instead of resizing the window
                                        
                                        // Get the actual work area (screen minus taskbar and other system elements)
                                        var workArea = SystemParameters.WorkArea;
                                        System.Diagnostics.Debug.WriteLine($"Work area: {workArea.Left},{workArea.Top} {workArea.Width}x{workArea.Height}");
                                        
                                        // Calculate new maximized area (work area minus StatusBar)
                                        var newLeft = (int)workArea.Left;
                                        var newTop = Math.Max((int)workArea.Top, STATUS_BAR_HEIGHT);
                                        var newWidth = (int)workArea.Width;
                                        var newHeight = (int)(workArea.Height - (newTop - workArea.Top));
                                        
                                        System.Diagnostics.Debug.WriteLine($"Adjusting maximized window '{windowTitle}' to fit below StatusBar: {newLeft},{newTop} {newWidth}x{newHeight}");
                                        
                                        // First restore to normal, then resize to custom "maximized" area
                                        NativeMethods.ShowWindow(hWnd, NativeMethods.SW_RESTORE);
                                        System.Threading.Thread.Sleep(50);
                                        
                                        // Set to custom maximized position that respects work area
                                        bool success = NativeMethods.SetWindowPos(hWnd, IntPtr.Zero,
                                            newLeft, newTop, newWidth, newHeight,
                                            NativeMethods.SWP_NOZORDER | NativeMethods.SWP_NOACTIVATE);
                                            
                                        System.Diagnostics.Debug.WriteLine($"Maximized window resize result: {success}");
                                    }
                                    else
                                    {
                                        // For regular windows, just move them down
                                        var newY = Math.Max(STATUS_BAR_HEIGHT, rect.Top);
                                        var newHeight = (rect.Bottom - rect.Top) - (newY - rect.Top);
                                        
                                        // Only resize if the window would still have reasonable height
                                        if (newHeight > 100)
                                        {
                                            System.Diagnostics.Debug.WriteLine($"Resizing regular window '{windowTitle}' to {rect.Left},{newY} {rect.Right - rect.Left}x{newHeight}");
                                            
                                            bool success = NativeMethods.SetWindowPos(hWnd, IntPtr.Zero,
                                                rect.Left, newY, rect.Right - rect.Left, newHeight,
                                                NativeMethods.SWP_NOZORDER | NativeMethods.SWP_NOACTIVATE);
                                                
                                            System.Diagnostics.Debug.WriteLine($"Resize result: {success}");
                                        }
                                        else
                                        {
                                            System.Diagnostics.Debug.WriteLine($"Skipping resize - new height {newHeight} too small");
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"EnumWindows: Skipping window '{windowTitle}' - filtered out or empty title");
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(windowTitle))
                        {
                            System.Diagnostics.Debug.WriteLine($"EnumWindows: Skipping window '{windowTitle}' - not visible");
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"EnumWindows: Error processing window {hWnd}: {ex.Message}");
                }
                return true; // Continue enumeration
            }, IntPtr.Zero);
            
            System.Diagnostics.Debug.WriteLine($"ResizeOverlappingWindows: Window enumeration completed. Found {_resizedWindows.Count} windows to resize.");
        }
        
        private void RestoreResizedWindows()
        {
            System.Diagnostics.Debug.WriteLine($"RestoreResizedWindows: Restoring {_resizedWindows.Count} windows");
            
            // Restore all windows to their original positions/sizes
            foreach (var kvp in _resizedWindows)
            {
                var hWnd = kvp.Key;
                var originalInfo = kvp.Value;
                
                try
                {
                    // Check if window still exists
                    if (NativeMethods.IsWindowVisible(hWnd))
                    {
                        if (originalInfo.WasMaximized)
                        {
                            System.Diagnostics.Debug.WriteLine($"Restoring window {hWnd} to maximized state");
                            // For previously maximized windows, restore to maximized state
                            // First restore to normal, then maximize to ensure correct behavior
                            NativeMethods.ShowWindow(hWnd, NativeMethods.SW_RESTORE);
                            System.Threading.Thread.Sleep(50);
                            NativeMethods.ShowWindow(hWnd, NativeMethods.SW_MAXIMIZE);
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"Restoring window {hWnd} to original position {originalInfo.X},{originalInfo.Y} {originalInfo.Width}x{originalInfo.Height}");
                            // For regular windows, restore original position and size
                            bool success = NativeMethods.SetWindowPos(hWnd, IntPtr.Zero,
                                originalInfo.X, originalInfo.Y, originalInfo.Width, originalInfo.Height,
                                NativeMethods.SWP_NOZORDER | NativeMethods.SWP_NOACTIVATE);
                            System.Diagnostics.Debug.WriteLine($"Restore result: {success}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to restore window {hWnd}: {ex.Message}");
                }
            }
            
            _resizedWindows.Clear();
        }
        
        private bool IsFullscreenWindow(IntPtr hWnd)
        {
            try
            {
                // Get window rectangle
                if (!NativeMethods.GetWindowRect(hWnd, out var rect))
                    return false;
                
                // Get screen dimensions
                var screenWidth = (int)SystemParameters.PrimaryScreenWidth;
                var screenHeight = (int)SystemParameters.PrimaryScreenHeight;
                
                // Check if window covers entire screen
                var windowWidth = rect.Right - rect.Left;
                var windowHeight = rect.Bottom - rect.Top;
                
                // Consider a window fullscreen if it covers the entire screen exactly (no tolerance for normal windows)
                bool isFullscreen = (rect.Left <= 0 && rect.Top <= 0 && 
                       windowWidth >= screenWidth && 
                       windowHeight >= screenHeight);
                       
                if (isFullscreen)
                {
                    // Get window title for debugging
                    var length = NativeMethods.GetWindowTextLength(hWnd);
                    var windowTitle = "";
                    if (length > 0)
                    {
                        var title = new StringBuilder(length + 1);
                        NativeMethods.GetWindowText(hWnd, title, title.Capacity);
                        windowTitle = title.ToString();
                    }
                    System.Diagnostics.Debug.WriteLine($"IsFullscreenWindow: Window '{windowTitle}' detected as fullscreen {rect.Left},{rect.Top} {windowWidth}x{windowHeight}");
                }
                
                return isFullscreen;
            }
            catch
            {
                return false;
            }
        }
        
        private bool IsMaximizedWindow(IntPtr hWnd)
        {
            try
            {
                // Check the window style for WS_MAXIMIZE flag
                var windowLong = NativeMethods.GetWindowLong(hWnd, NativeMethods.GWL_STYLE);
                return (windowLong & NativeMethods.WS_MAXIMIZE) != 0;
            }
            catch
            {
                return false;
            }
        }
        
        // Win32 API for getting cursor position
        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }
        
        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);
        
        protected override void OnClosed(EventArgs e)
        {
            _mouseTrackingTimer?.Stop();
            _autoHideTimer?.Stop();
            base.OnClosed(e);
        }
    }
}
