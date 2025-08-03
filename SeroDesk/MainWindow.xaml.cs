using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using SeroDesk.Platform;
using SeroDesk.Services;
using SeroDesk.ViewModels;
using SeroDesk.Views;

namespace SeroDesk
{
    public partial class MainWindow : Window
    {
        private MainViewModel _viewModel;
        private GestureRecognizer _gestureRecognizer;
        private LaunchpadWindow? _launchpadWindow;
        
        public MainWindow()
        {
            InitializeComponent();
            
            _viewModel = new MainViewModel();
            DataContext = _viewModel;
            
            _gestureRecognizer = new GestureRecognizer();
            _gestureRecognizer.SwipeDetected += OnSwipeDetected;
            _gestureRecognizer.PinchDetected += OnPinchDetected;
            
            Loaded += OnWindowLoaded;
        }
        
        private void LoadWallpaper()
        {
            try
            {
                var wallpaperPath = WallpaperService.Instance.GetCurrentWallpaperPath();
                if (!string.IsNullOrEmpty(wallpaperPath) && System.IO.File.Exists(wallpaperPath))
                {
                    WallpaperImage.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(wallpaperPath));
                }
                else
                {
                    // Fallback gradient
                    var gradient = new LinearGradientBrush();
                    gradient.StartPoint = new Point(0, 0);
                    gradient.EndPoint = new Point(1, 1);
                    gradient.GradientStops.Add(new GradientStop(Color.FromRgb(0, 122, 255), 0));
                    gradient.GradientStops.Add(new GradientStop(Color.FromRgb(0, 85, 180), 1));
                    DesktopLayer.Background = gradient;
                }
            }
            catch
            {
                // Fallback gradient on error
                var gradient = new LinearGradientBrush();
                gradient.StartPoint = new Point(0, 0);
                gradient.EndPoint = new Point(1, 1);
                gradient.GradientStops.Add(new GradientStop(Color.FromRgb(0, 122, 255), 0));
                gradient.GradientStops.Add(new GradientStop(Color.FromRgb(0, 85, 180), 1));
                DesktopLayer.Background = gradient;
            }
        }
        
        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            // Setup window for overlay mode
            SetupOverlayWindow();
            
            // Load wallpaper
            LoadWallpaper();
            
            // Initialize desktop components
            InitializeDesktop();
            
            // Connect status bar events
            StatusBar.LeftSideClicked += (s, args) => ShowNotificationCenter();
            StatusBar.RightSideClicked += (s, args) => ShowControlCenter();
            
            // Start animation
            FadeIn();
        }
        
        private void SetupOverlayWindow()
        {
            // Get window handle
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            
            // Set window to desktop layer (behind all normal windows)
            WindowsIntegration.SetWindowAsDesktopChild(hwnd);
            
            // Register as shell replacement
            WindowsIntegration.RegisterAsShell(hwnd);
            
            // This window should be interactive (NOT click-through)
            // Only set it to bottom of Z-order, but keep it interactive
        }
        
        private void InitializeDesktop()
        {
            // Load desktop icons
            _viewModel.LoadDesktopIcons();
            
            // Load widgets
            _viewModel.LoadWidgets(WidgetContainer);
            
            // Initialize SeroDock
            SeroDock.Initialize();
            
            // Initialize DesktopLaunchpad (SpringBoard replacement)  
            // Note: DesktopLaunchpad gets DataContext from MainViewModel.Launchpad
            
            // Create separate launchpad window (TopMost)
            _launchpadWindow = new LaunchpadWindow();
            
            // Load apps for SpringBoard
            _viewModel.LoadAllAppsForSpringBoard();
        }
        
        private void FadeIn()
        {
            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(500),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            
            MainGrid.BeginAnimation(OpacityProperty, fadeIn);
        }
        
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Handle global hotkeys
            if (e.Key == Key.Escape && Keyboard.Modifiers == ModifierKeys.Shift)
            {
                // Shift+Esc to exit
                Application.Current.Shutdown();
            }
            else if (e.Key == Key.D && Keyboard.Modifiers == ModifierKeys.Windows)
            {
                // Win+D to show/hide desktop
                ToggleDesktopVisibility();
            }
            
            // Block Windows hotkeys to prevent system interference
            if (Keyboard.Modifiers == ModifierKeys.Windows)
            {
                switch (e.Key)
                {
                    case Key.R: // Win+R (Run dialog)
                    case Key.L: // Win+L (Lock screen)  
                    case Key.Tab: // Win+Tab (Task view)
                    case Key.X: // Win+X (Power user menu)
                        e.Handled = true;
                        break;
                }
            }
        }
        
        private void GestureOverlay_ManipulationStarting(object sender, ManipulationStartingEventArgs e)
        {
            e.ManipulationContainer = GestureOverlay;
            e.Mode = ManipulationModes.All;
            _gestureRecognizer.OnManipulationStarting(e);
        }
        
        private void GestureOverlay_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            _gestureRecognizer.OnManipulationDelta(e);
        }
        
        private void GestureOverlay_ManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            _gestureRecognizer.OnManipulationCompleted(e);
        }
        
        private void OnSwipeDetected(SwipeDirection direction)
        {
            var startX = _gestureRecognizer.StartPoint.X;
            var startY = _gestureRecognizer.StartPoint.Y;
            var screenWidth = ActualWidth;
            var screenHeight = ActualHeight;
            
            switch (direction)
            {
                case SwipeDirection.Up:
                    // Swipe up from bottom - bring Shell to foreground
                    if (startY > screenHeight - 200)
                    {
                        BringShellToForeground();
                        ShowLaunchpad();
                    }
                    break;
                    
                case SwipeDirection.Down:
                    // Swipe down from top - show Notification Center or Control Center
                    if (startY < 100) // Started from top of screen
                    {
                        if (startX < screenWidth / 2)
                        {
                            // Left half - show Notification Center
                            ShowNotificationCenter();
                        }
                        else
                        {
                            // Right half - show Control Center
                            ShowControlCenter();
                        }
                    }
                    // Regular swipe down - hide overlays
                    else if (_launchpadWindow?.Visibility == Visibility.Visible)
                    {
                        HideLaunchpad();
                        SendShellToBack();
                    }
                    else if (NotificationCenter.IsVisible || ControlCenter.IsVisible)
                    {
                        HideNotificationCenter();
                        HideControlCenter();
                    }
                    else
                    {
                        MinimizeAllWindows();
                    }
                    break;
            }
        }
        
        private void BringShellToForeground()
        {
            // Bring this window and dock to foreground
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            WindowsIntegration.SetWindowAlwaysOnTop(hwnd);
            this.Activate();
        }
        
        private void SendShellToBack()
        {
            // Send this window back to desktop layer
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            WindowsIntegration.SetWindowAsDesktopChild(hwnd);
        }
        
        private void OnPinchDetected(double scaleFactor)
        {
            // Pinch to zoom desktop icons
            // Note: IconGrid was replaced by DesktopLaunchpad (SeroLaunchpad)
            // Pinch scaling can be implemented in SeroLaunchpad if needed
        }
        
        private void ToggleDesktopVisibility()
        {
            var fadeAnimation = new DoubleAnimation
            {
                To = DesktopLayer.Opacity > 0 ? 0 : 0.95,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };
            
            DesktopLayer.BeginAnimation(OpacityProperty, fadeAnimation);
        }
        
        public void ShowLaunchpad()
        {
            _launchpadWindow?.ShowLaunchpad();
        }
        
        public void HideLaunchpad()
        {
            _launchpadWindow?.HideLaunchpad();
        }
        
        private void ShowAllWindows()
        {
            WindowManager.Instance.ShowWindowSwitcher();
        }
        
        private void MinimizeAllWindows()
        {
            WindowManager.Instance.MinimizeAllWindows();
        }
        
        public void ShowNotificationCenter()
        {
            // Hide Control Center if visible
            HideControlCenter();
            
            // Show Notification Center
            NotificationCenter.Show();
        }
        
        private void HideNotificationCenter()
        {
            NotificationCenter.Hide();
        }
        
        public void ShowControlCenter()
        {
            // Hide Notification Center if visible
            HideNotificationCenter();
            
            // Show Control Center
            ControlCenter.Show();
        }
        
        private void HideControlCenter()
        {
            ControlCenter.Hide();
        }
    }
}