using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Text;
using SeroDesk.Platform;

namespace SeroDesk.Views
{
    public partial class StatusBarWindow : Window
    {
        private DispatcherTimer _mouseTrackingTimer;
        private DispatcherTimer _autoHideTimer;
        private bool _isVisible = true;
        private bool _isAnimating = false;
        private bool _isOnDesktop = true; // Track if we're currently on desktop/launchpad
        private const int STATUS_BAR_HEIGHT = 32;
        private const int MOUSE_ACTIVATION_AREA = 5; // Pixels from top of screen
        
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
            
            // Start mouse tracking
            _mouseTrackingTimer.Start();
            
            // Initially show the status bar
            ShowStatusBar();
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
            
            slideDown.Completed += (s, e) => _isAnimating = false;
            
            var translateTransform = new System.Windows.Media.TranslateTransform();
            this.RenderTransform = translateTransform;
            
            translateTransform.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, slideDown);
            this.BeginAnimation(OpacityProperty, fadeIn);
        }
        
        private void HideStatusBar()
        {
            if (!_isVisible || _isAnimating) return;
            
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
