using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
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
            
            // Initialize timers
            _mouseTrackingTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50) // Check mouse position every 50ms
            };
            _mouseTrackingTimer.Tick += MouseTrackingTimer_Tick;
            
            _hideTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3) // Hide after 3 seconds of no mouse activity
            };
            _hideTimer.Tick += HideTimer_Tick;
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
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;
            
            // Position at bottom center
            this.Width = 500; // Initial width, will adjust based on content
            this.Height = DOCK_HEIGHT;
            this.Left = (screenWidth - this.Width) / 2;
            this.Top = screenHeight - DOCK_HEIGHT;
            
            // Ensure window stays within screen bounds
            if (this.Left < 0) this.Left = 0;
            if (this.Left + this.Width > screenWidth) this.Left = screenWidth - this.Width;
        }
        
        private void MouseTrackingTimer_Tick(object? sender, EventArgs e)
        {
            if (_isAnimating) return;
            
            var mousePosition = GetCursorPosition();
            var screenHeight = SystemParameters.PrimaryScreenHeight;
            var dockArea = new Rect(this.Left - 50, screenHeight - 100, this.Width + 100, 100);
            
            if (dockArea.Contains(mousePosition))
            {
                // Mouse is near dock area
                if (!_isVisible)
                {
                    ShowDock();
                }
                
                // Reset hide timer
                _hideTimer.Stop();
                _hideTimer.Start();
            }
            else if (_isVisible && mousePosition.Y < screenHeight - 150)
            {
                // Mouse is far from dock area
                _hideTimer.Stop();
                _hideTimer.Start();
            }
        }
        
        private void HideTimer_Tick(object? sender, EventArgs e)
        {
            if (_isVisible && !_isAnimating && !IsMouseOverDock())
            {
                HideDock();
            }
            _hideTimer.Stop();
        }
        
        private bool IsMouseOverDock()
        {
            var mousePosition = GetCursorPosition();
            var dockBounds = new Rect(this.Left, this.Top, this.Width, this.Height);
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