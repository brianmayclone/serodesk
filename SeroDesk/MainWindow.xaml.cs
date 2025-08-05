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
    /// <summary>
    /// MainWindow serves as the primary desktop shell replacement for Windows 11.
    /// This class coordinates the iOS-inspired touch interface, manages gesture recognition,
    /// and orchestrates the interaction between various shell components including the dock,
    /// launchpad, notification center, and widget system.
    /// </summary>
    /// <remarks>
    /// The MainWindow architecture follows these principles:
    /// - Acts as a full-screen desktop overlay positioned behind normal windows
    /// - Implements touch gesture recognition for iOS-like interaction patterns
    /// - Manages separate windows for dock and launchpad to ensure proper Z-ordering
    /// - Coordinates wallpaper display and widget management
    /// - Handles system integration through Windows shell APIs
    /// 
    /// Key components managed:
    /// - Desktop wallpaper rendering and fallback gradients
    /// - Widget container for desktop widgets (clock, weather, etc.)
    /// - Status bar with notification and control center access
    /// - Gesture overlay for touch input processing
    /// - Separate dock and launchpad windows for proper layering
    /// </remarks>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Primary view model containing all business logic and data for the main window.
        /// Manages desktop icons, widget collections, and launchpad content through MVVM pattern.
        /// </summary>
        private MainViewModel _viewModel;
        
        /// <summary>
        /// Gesture recognition engine for processing touch input and converting raw touch
        /// events into semantic gestures (swipes, pinches) for iOS-like interaction.
        /// </summary>
        private GestureRecognizer _gestureRecognizer;
        
        /// <summary>
        /// Separate window for the iOS-style launchpad (SpringBoard) that displays
        /// all installed applications in a grid layout. Managed as a separate window
        /// to ensure proper Z-ordering and always-on-top behavior.
        /// </summary>
        private LaunchpadWindow? _launchpadWindow;
        
        /// <summary>
        /// Separate window for the macOS-style dock that shows running applications
        /// and favorites. Maintained as an independent window to ensure it stays
        /// above all other content while remaining accessible.
        /// </summary>
        private DockWindow? _dockWindow;
        
        /// <summary>
        /// StatusBar window for iOS-style status bar with automatic window resizing
        /// </summary>
        private StatusBarWindow? _statusBarWindow;
        
        /// <summary>
        /// View model for the dock functionality, accessible to other components
        /// for adding/removing apps from the dock.
        /// </summary>
        public DockViewModel? DockViewModel => _dockWindow?.Dock?.DataContext as DockViewModel;
        
        /// <summary>
        /// Initializes a new instance of the MainWindow class and sets up the core shell infrastructure.
        /// This constructor establishes the MVVM data context, configures gesture recognition,
        /// and prepares the window for shell replacement functionality.
        /// </summary>
        /// <remarks>
        /// Constructor initialization sequence:
        /// 1. Initializes WPF component tree from XAML
        /// 2. Creates and binds the main view model for MVVM data binding
        /// 3. Sets up gesture recognition for touch input processing
        /// 4. Configures event handlers for window lifecycle management
        /// 
        /// The gesture recognizer is configured to detect:
        /// - Swipe gestures for navigation (up/down/left/right)
        /// - Pinch gestures for zoom operations on widgets
        /// - Multi-touch interactions for advanced manipulation
        /// </remarks>
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
        
        /// <summary>
        /// Loads and displays the current Windows desktop wallpaper or applies a fallback gradient.
        /// This method integrates with Windows wallpaper service to maintain visual consistency
        /// with the user's desktop theme while providing a reliable fallback for error scenarios.
        /// </summary>
        /// <remarks>
        /// Wallpaper loading strategy:
        /// 1. Attempts to retrieve current wallpaper path from Windows registry/settings
        /// 2. Validates file existence and loads as WPF BitmapImage if available
        /// 3. Falls back to an attractive blue gradient if wallpaper is unavailable
        /// 4. Applies the same gradient if any exception occurs during loading
        /// 
        /// The fallback gradient (blue to darker blue) provides:
        /// - Visual consistency with Windows 11 design language
        /// - Sufficient contrast for desktop icons and widgets
        /// - Professional appearance when wallpaper is not accessible
        /// </remarks>
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
                    // Fallback gradient when wallpaper is not available
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
                // Fallback gradient on any error (file corruption, access denied, etc.)
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
            
            // Status bar events are now handled in the separate StatusBarWindow
            
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
            // Desktop functionality is disabled - icons are now managed through LaunchPad
            // _viewModel.LoadDesktopIcons(); // DISABLED: Use Launchpad instead
            
            // Load widgets
            _viewModel.LoadWidgets(WidgetContainer);
            
            // Create separate dock window (Always-on-Top)
            _dockWindow = new DockWindow();
            _dockWindow.Show();
            
            // Initialize DesktopLaunchpad (SpringBoard replacement)  
            // Note: DesktopLaunchpad gets DataContext from MainViewModel.Launchpad
            DesktopLaunchpad.DataContext = _viewModel.Launchpad;
            DesktopLaunchpad.Initialize(); // Initialize AFTER setting DataContext
            
            // Create separate launchpad window (TopMost)
            _launchpadWindow = new LaunchpadWindow();
            
            // Share the same ViewModel between desktop and window launchpad (BEFORE loading apps)
            _launchpadWindow.Launchpad.DataContext = _viewModel.Launchpad;
            _launchpadWindow.Launchpad.Initialize(); // Initialize AFTER setting DataContext
            
            // Load apps for SpringBoard (async - will fill the shared ViewModel)
            _viewModel.LoadAllAppsForSpringBoard();
            
            // Initialize Windows key hook
            WindowsKeyHook.Initialize(this);
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
            // Windows key should do EXACTLY the same as Home button in dock
            _dockWindow?.Dock?.TriggerHomeButtonAction();
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
        
        private void MinimizeOtherWindows()
        {
            // Store dock state to prevent it from being minimized
            var dockWasVisible = _dockWindow?.IsVisible ?? false;
            
            // First minimize all windows (including SeroDesk temporarily)
            WindowManager.Instance.MinimizeAllWindows();
            
            // Then immediately bring SeroDesk components back to foreground
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                // Restore SeroDesk windows
                this.WindowState = WindowState.Maximized;
                this.Show();
                this.Activate();
                
                // ALWAYS restore dock - it must never be minimized
                if (_dockWindow != null)
                {
                    _dockWindow.Show();
                    _dockWindow.WindowState = WindowState.Normal;
                    _dockWindow.Topmost = true; // Ensure dock stays on top
                }
                
            }), System.Windows.Threading.DispatcherPriority.Normal);
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
        
        protected override void OnClosed(EventArgs e)
        {
            // Clean up Windows key hook
            WindowsKeyHook.Shutdown();
            
            // Clean up dock window
            _dockWindow?.Close();
            _dockWindow = null;
            
            // Clean up launchpad window
            _launchpadWindow?.Close();
            _launchpadWindow = null;
            
            base.OnClosed(e);
        }
    }
}