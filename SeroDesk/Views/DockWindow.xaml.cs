using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using SeroDesk.Platform;

namespace SeroDesk.Views
{
    public partial class DockWindow : Window
    {
        private DispatcherTimer _mouseTrackingTimer;
        private DispatcherTimer _hideTimer;
        private bool _isVisible = true;
        private bool _isAnimating = false;
        private const int DOCK_HEIGHT = 90;
        private const int HIDDEN_POSITION_OFFSET = 75; // How much to hide (leave small strip visible)
        
        public DockWindow()
        {
            InitializeComponent();
            
            Loaded += DockWindow_Loaded;
            SizeChanged += DockWindow_SizeChanged;
            
            // Initialize timers
            _mouseTrackingTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50) // Check mouse position every 50ms
            };
            _mouseTrackingTimer.Tick += MouseTrackingTimer_Tick;
            
            _hideTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2) // Hide after 2 seconds of no mouse activity
            };
            _hideTimer.Tick += HideTimer_Tick;
        }
        
        private void DockWindow_SourceInitialized(object sender, EventArgs e)
        {
            // No acrylic effects - keep it simple and transparent
        }
        
        private void DockWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Recenter the dock when its size changes
            if (IsLoaded)
            {
                CenterDockHorizontally();
            }
        }
        
        private void DockWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Position dock at bottom center of primary screen
            PositionDockAtBottom();
            
            // Initialize the dock component
            Dock.Initialize();
            
            // Start mouse tracking
            _mouseTrackingTimer.Start();
            _hideTimer.Start();
        }
        
        private void PositionDockAtBottom()
        {
            var screenHeight = SystemParameters.PrimaryScreenHeight;
            
            // Position at bottom center - don't set width, let SizeToContent handle it
            this.Height = DOCK_HEIGHT;
            this.Top = screenHeight - DOCK_HEIGHT;
            
            // Center horizontally after the window has determined its actual size
            this.Dispatcher.BeginInvoke(new Action(() => CenterDockHorizontally()), DispatcherPriority.Loaded);
        }
        
        private void CenterDockHorizontally()
        {
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            
            // Center the dock horizontally based on its actual width
            this.Left = (screenWidth - this.ActualWidth) / 2;
            
            // Ensure window stays within screen bounds
            if (this.Left < 0) this.Left = 0;
            if (this.Left + this.ActualWidth > screenWidth) this.Left = screenWidth - this.ActualWidth;
        }
        
        private void MouseTrackingTimer_Tick(object? sender, EventArgs e)
        {
            if (_isAnimating) return;
            
            var mousePosition = GetCursorPosition();
            var screenHeight = SystemParameters.PrimaryScreenHeight;
            var dockArea = new Rect(this.Left - 50, screenHeight - 150, this.ActualWidth + 100, 150);
            
            if (dockArea.Contains(mousePosition))
            {
                // Mouse is near dock area
                if (!_isVisible)
                {
                    ShowDock();
                }
                
                // Reset hide timer while mouse is in dock area
                _hideTimer.Stop();
                _hideTimer.Start();
            }
            else if (_isVisible)
            {
                // Mouse is away from dock area - start hide timer if not already running
                if (!_hideTimer.IsEnabled)
                {
                    _hideTimer.Start();
                }
            }
        }
        
        private void HideTimer_Tick(object? sender, EventArgs e)
        {
            // Double-check that mouse is not over dock before hiding
            if (_isVisible && !_isAnimating && !IsMouseOverDock())
            {
                var mousePosition = GetCursorPosition();
                var screenHeight = SystemParameters.PrimaryScreenHeight;
                
                // Only hide if mouse is not in the dock area
                if (mousePosition.Y < screenHeight - 150)
                {
                    HideDock();
                }
            }
            
            _hideTimer.Stop();
            _hideTimer.Stop();
        }
        
        private bool IsMouseOverDock()
        {
            var mousePosition = GetCursorPosition();
            var screenHeight = SystemParameters.PrimaryScreenHeight;
            
            // Expanded dock bounds to include a buffer area
            var dockBounds = new Rect(this.Left - 25, screenHeight - 120, this.ActualWidth + 50, 120);
            return dockBounds.Contains(mousePosition);
        }
        
        private Point GetCursorPosition()
        {
            var point = new POINT();
            GetCursorPos(out point);
            return new Point(point.X, point.Y);
        }
        
        private void ShowDock()
        {
            if (_isVisible || _isAnimating) return;
            
            _isAnimating = true;
            _isVisible = true;
            
            var screenHeight = SystemParameters.PrimaryScreenHeight;
            var targetTop = screenHeight - DOCK_HEIGHT;
            
            var slideUpAnimation = new DoubleAnimation
            {
                From = this.Top,
                To = targetTop,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            
            var fadeInAnimation = new DoubleAnimation
            {
                From = this.Opacity,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(200)
            };
            
            slideUpAnimation.Completed += (s, e) => _isAnimating = false;
            
            this.BeginAnimation(Window.TopProperty, slideUpAnimation);
            this.BeginAnimation(Window.OpacityProperty, fadeInAnimation);
        }
        
        private void HideDock()
        {
            if (!_isVisible || _isAnimating) return;
            
            _isAnimating = true;
            _isVisible = false;
            
            var screenHeight = SystemParameters.PrimaryScreenHeight;
            var targetTop = screenHeight - (DOCK_HEIGHT - HIDDEN_POSITION_OFFSET);
            
            var slideDownAnimation = new DoubleAnimation
            {
                From = this.Top,
                To = targetTop,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            
            var fadeOutAnimation = new DoubleAnimation
            {
                From = this.Opacity,
                To = 0.7,
                Duration = TimeSpan.FromMilliseconds(200)
            };
            
            slideDownAnimation.Completed += (s, e) => _isAnimating = false;
            
            this.BeginAnimation(Window.TopProperty, slideDownAnimation);
            this.BeginAnimation(Window.OpacityProperty, fadeOutAnimation);
        }
        
        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);
            
            // Reset hide timer when mouse enters dock
            _hideTimer.Stop();
            
            if (!_isVisible)
            {
                ShowDock();
            }
        }
        
        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            
            // Start hide timer when mouse leaves dock
            _hideTimer.Start();
        }
        
        /// <summary>
        /// Public method to show the dock (used by global Windows key hook)
        /// </summary>
        public void ShowDockForced()
        {
            ShowDock();
        }
        
        /// <summary>
        /// Public method to check if dock is currently visible
        /// </summary>
        public bool IsDockVisible => _isVisible;
        
        protected override void OnClosed(EventArgs e)
        {
            _mouseTrackingTimer?.Stop();
            _hideTimer?.Stop();
            base.OnClosed(e);
        }
        
        // P/Invoke for getting cursor position
        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);
        
        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }
    }
}