using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using SeroDesk.Platform;
using SeroDesk.ViewModels;

namespace SeroDesk.Views
{
    public partial class SeroDock : UserControl
    {
        private DockViewModel? _viewModel;
        private List<IntPtr> _minimizedWindows = new List<IntPtr>();
        private bool _isDesktopMode = false;
        
        /// <summary>
        /// Public property to check if dock is in desktop mode
        /// </summary>
        public bool IsInDesktopMode => _isDesktopMode;
        
        public SeroDock()
        {
            InitializeComponent();
            
            Loaded += (s, e) =>
            {
                _viewModel = DataContext as DockViewModel;
                ApplyBackdropMaterial();
                LoadSystemIcons();
            };
        }
        
        public void Initialize()
        {
            _viewModel = new DockViewModel();
            DataContext = _viewModel;
            
            _viewModel.StartMonitoringWindows();
            
            // Load system icons with delay to ensure UI is fully loaded
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                ApplyBackdropMaterial();
                LoadSystemIcons();
            }), System.Windows.Threading.DispatcherPriority.Loaded);
            
            AnimateIn();
        }
        
        private void LoadSystemIcons()
        {
            try
            {
                // Icons are inside ControlTemplates, so FindName won't work.
                // We must walk the visual tree to find Image elements by x:Name.
                LoadSystemIconDelayed("ExplorerIcon", () =>
                {
                    var icon = IconExtractor.GetSystemIcon(Platform.SystemIconType.Computer)
                            ?? IconExtractor.GetIconForFile(@"C:\Windows\explorer.exe", true);
                    return icon;
                });

                LoadSystemIconDelayed("RecycleBinIcon", () =>
                {
                    var icon = IconExtractor.GetSystemIcon(Platform.SystemIconType.RecycleBin)
                            ?? IconExtractor.GetIconForFile(@"C:\Windows\System32\shell32.dll", true);
                    return icon;
                });
            }
            catch (Exception ex)
            {
                Services.Logger.Error("LoadSystemIcons failed", ex);
            }
        }

        private void ApplyBackdropMaterial()
        {
            try
            {
                DockBackdropMaterial.Fill = CreateBackdropBrush();
            }
            catch (Exception ex)
            {
                Services.Logger.Warn($"Failed to apply dock backdrop material: {ex.Message}");
            }
        }

        private Brush CreateBackdropBrush()
        {
            if (Application.Current?.MainWindow?.DataContext is MainViewModel mainViewModel &&
                mainViewModel.Launchpad.CurrentWallpaper is ImageBrush wallpaperBrush)
            {
                var clonedBrush = wallpaperBrush.CloneCurrentValue();
                clonedBrush.Stretch = Stretch.UniformToFill;
                clonedBrush.AlignmentX = AlignmentX.Center;
                clonedBrush.AlignmentY = AlignmentY.Bottom;
                return clonedBrush;
            }

            return new LinearGradientBrush(
                new GradientStopCollection
                {
                    new(Color.FromArgb(0xD0, 0x74, 0x84, 0x9B), 0),
                    new(Color.FromArgb(0xC8, 0x36, 0x45, 0x59), 1)
                },
                new Point(0, 0),
                new Point(1, 1));
        }

        private void LoadSystemIconDelayed(string imageName, Func<System.Windows.Media.ImageSource?> iconLoader)
        {
            // Delay to ensure ControlTemplates have been applied
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    var image = FindVisualChildByName<System.Windows.Controls.Image>(this, imageName);
                    if (image != null)
                    {
                        var iconSource = iconLoader();
                        if (iconSource != null)
                        {
                            image.Source = iconSource;
                            Services.Logger.Debug($"Loaded system icon: {imageName}");
                        }
                    }
                    else
                    {
                        Services.Logger.Warn($"Image '{imageName}' not found in visual tree");
                    }
                }
                catch (Exception ex)
                {
                    Services.Logger.Error($"Failed to load {imageName}", ex);
                }
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private static T? FindVisualChildByName<T>(DependencyObject parent, string name) where T : FrameworkElement
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T element && element.Name == name)
                    return element;
                var result = FindVisualChildByName<T>(child, name);
                if (result != null) return result;
            }
            return null;
        }
        
        private void AnimateIn()
        {
            var slideUp = new DoubleAnimation
            {
                From = 100,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(800),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            
            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(600),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            
            var translateTransform = new System.Windows.Media.TranslateTransform();
            DockBackground.RenderTransform = translateTransform;
            
            translateTransform.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, slideUp);
            DockBackground.BeginAnimation(OpacityProperty, fadeIn);
        }
        
        private void DockIcon_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is WindowInfo window)
            {
                try
                {
                    if (window.IsMinimized)
                    {
                        // Restore and bring to foreground
                        NativeMethods.ShowWindow(window.Handle, NativeMethods.SW_RESTORE);
                        NativeMethods.SetForegroundWindow(window.Handle);
                    }
                    else
                    {
                        var foregroundWindow = NativeMethods.GetForegroundWindow();
                        if (foregroundWindow == window.Handle)
                        {
                            // Minimize if already focused
                            NativeMethods.ShowWindow(window.Handle, NativeMethods.SW_MINIMIZE);
                        }
                        else
                        {
                            // Bring to foreground if not focused
                            NativeMethods.SetForegroundWindow(window.Handle);
                            NativeMethods.ShowWindow(window.Handle, NativeMethods.SW_RESTORE);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to switch to window: {ex.Message}", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
                
                // Bounce animation
                PlayBounceAnimation(button);
            }
        }
        
        private void DockIcon_MouseEnter(object sender, MouseEventArgs e)
        {
            // Magnification effect is handled in XAML triggers
        }
        
        private void DockIcon_MouseLeave(object sender, MouseEventArgs e)
        {
            // Magnification effect is handled in XAML triggers
        }
        
        private void DockIcon_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is Button button && button.Tag is WindowInfo window)
            {
                // Store the current window info for context menu actions
                button.ContextMenu.Tag = window;
                
                // Update context menu items based on window state
                UpdateContextMenuItems(button.ContextMenu, window);
                
                e.Handled = true;
            }
        }
        
        private void UpdateContextMenuItems(ContextMenu contextMenu, WindowInfo window)
        {
            foreach (MenuItem item in contextMenu.Items.OfType<MenuItem>())
            {
                switch (item.Header.ToString())
                {
                    case "Close Window":
                        item.IsEnabled = window.IsRunning;
                        break;
                    case "Open":
                        item.IsEnabled = !window.IsRunning || window.IsMinimized;
                        break;
                }
            }
        }
        
        private void FinderIcon_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Launch File Explorer
                var process = System.Diagnostics.Process.Start("explorer.exe");
                
                if (process != null)
                {
                    // Wait for process to start and bring to foreground
                    System.Threading.Thread.Sleep(500);
                    
                    if (!process.HasExited && process.MainWindowHandle != IntPtr.Zero)
                    {
                        NativeMethods.SetForegroundWindow(process.MainWindowHandle);
                        NativeMethods.ShowWindow(process.MainWindowHandle, NativeMethods.SW_RESTORE);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to launch Explorer: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
            PlayBounceAnimation(sender as Button);
        }
        
        
        private void TrashIcon_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Open Recycle Bin
                var process = System.Diagnostics.Process.Start("explorer.exe", "shell:RecycleBinFolder");
                
                if (process != null)
                {
                    // Wait for process to start and bring to foreground
                    System.Threading.Thread.Sleep(500);
                    
                    if (!process.HasExited && process.MainWindowHandle != IntPtr.Zero)
                    {
                        NativeMethods.SetForegroundWindow(process.MainWindowHandle);
                        NativeMethods.ShowWindow(process.MainWindowHandle, NativeMethods.SW_RESTORE);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open Recycle Bin: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
            PlayBounceAnimation(sender as Button);
        }
        
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Open Settings window
                SettingsWindow.ShowSettings();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open Settings: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
            PlayBounceAnimation(sender as Button);
        }
        
        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_isDesktopMode)
                {
                    // Restore previously minimized windows
                    RestoreMinimizedWindows();
                    _isDesktopMode = false;
                }
                else
                {
                    // Show Desktop functionality - minimize all windows
                    MinimizeAllWindows();
                    _isDesktopMode = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to toggle desktop mode: {ex.Message}");
            }
            
            PlayBounceAnimation(sender as Button);
        }
        
        private void MinimizeAllWindows()
        {
            // Clear previous list
            _minimizedWindows.Clear();
            
            // Enumerate all windows and minimize visible ones
            NativeMethods.EnumWindows((hWnd, lParam) =>
            {
                if (NativeMethods.IsWindowVisible(hWnd))
                {
                    // Get window title to filter out system windows
                    var length = NativeMethods.GetWindowTextLength(hWnd);
                    if (length > 0)
                    {
                        var title = new StringBuilder(length + 1);
                        NativeMethods.GetWindowText(hWnd, title, title.Capacity);
                        
                        // Skip our own windows and system windows
                        var windowTitle = title.ToString();
                        if (!string.IsNullOrEmpty(windowTitle) && 
                            !windowTitle.Contains("SeroDesk") &&
                            !windowTitle.Contains("Task View") &&
                            !windowTitle.Contains("Start") &&
                            windowTitle != "Program Manager")
                        {
                            // Check if window is not already minimized
                            var windowLong = NativeMethods.GetWindowLong(hWnd, NativeMethods.GWL_STYLE);
                            if ((windowLong & NativeMethods.WS_MINIMIZE) == 0)
                            {
                                // Track this window before minimizing
                                _minimizedWindows.Add(hWnd);
                                NativeMethods.ShowWindow(hWnd, NativeMethods.SW_MINIMIZE);
                            }
                        }
                    }
                }
                return true; // Continue enumeration
            }, IntPtr.Zero);
        }
        
        private void RestoreMinimizedWindows()
        {
            // Restore all previously minimized windows
            foreach (var hWnd in _minimizedWindows)
            {
                try
                {
                    // Check if the window still exists and is minimized
                    if (NativeMethods.IsWindowVisible(hWnd))
                    {
                        var windowLong = NativeMethods.GetWindowLong(hWnd, NativeMethods.GWL_STYLE);
                        if ((windowLong & NativeMethods.WS_MINIMIZE) != 0)
                        {
                            NativeMethods.ShowWindow(hWnd, NativeMethods.SW_RESTORE);
                            NativeMethods.SetForegroundWindow(hWnd);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to restore window {hWnd}: {ex.Message}");
                }
            }
            
            // Clear the list after restoration
            _minimizedWindows.Clear();
        }
        
        private void PlayBounceAnimation(Button? button)
        {
            if (button == null) return;

            // Ensure we have a ScaleTransform we can animate
            var scaleTransform = button.RenderTransform as System.Windows.Media.ScaleTransform;
            if (scaleTransform == null)
            {
                scaleTransform = new System.Windows.Media.ScaleTransform(1, 1);
                button.RenderTransform = scaleTransform;
                button.RenderTransformOrigin = new Point(0.5, 0.5);
            }

            var bounceAnim = new DoubleAnimation
            {
                To = 1.3,
                Duration = TimeSpan.FromMilliseconds(100),
                AutoReverse = true,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            scaleTransform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, bounceAnim);
            scaleTransform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, bounceAnim);
        }
        
        
        public void AutoHide()
        {
            var slideDown = new DoubleAnimation
            {
                To = 100,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            
            var fadeOut = new DoubleAnimation
            {
                To = 0.3,
                Duration = TimeSpan.FromMilliseconds(300)
            };
            
            var translateTransform = DockBackground.RenderTransform as System.Windows.Media.TranslateTransform;
            if (translateTransform == null)
            {
                translateTransform = new System.Windows.Media.TranslateTransform();
                DockBackground.RenderTransform = translateTransform;
            }
            
            translateTransform.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, slideDown);
            DockBackground.BeginAnimation(OpacityProperty, fadeOut);
            
            DockIndicator.Visibility = Visibility.Visible;
        }
        
        public void AutoShow()
        {
            var slideUp = new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            
            var fadeIn = new DoubleAnimation
            {
                To = 1,
                Duration = TimeSpan.FromMilliseconds(300)
            };
            
            var translateTransform = DockBackground.RenderTransform as System.Windows.Media.TranslateTransform;
            if (translateTransform == null)
            {
                translateTransform = new System.Windows.Media.TranslateTransform();
                DockBackground.RenderTransform = translateTransform;
            }
            
            translateTransform.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, slideUp);
            DockBackground.BeginAnimation(OpacityProperty, fadeIn);
            
            DockIndicator.Visibility = Visibility.Collapsed;
        }
        
        /// <summary>
        /// Public method to trigger the home button action (used by global Windows key hook)
        /// </summary>
        public void TriggerHomeButtonAction()
        {
            try
            {
                if (_isDesktopMode)
                {
                    // Restore previously minimized windows
                    RestoreMinimizedWindows();
                    _isDesktopMode = false;
                }
                else
                {
                    // Show Desktop functionality - minimize all windows
                    MinimizeAllWindows();
                    _isDesktopMode = true;
                    
                    // Focus on LaunchPad search box after showing desktop
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        var mainWindow = Application.Current.MainWindow as MainWindow;
                        if (mainWindow?.DesktopLaunchpad?.SearchBox != null)
                        {
                            mainWindow.DesktopLaunchpad.SearchBox.Focus();
                            Keyboard.Focus(mainWindow.DesktopLaunchpad.SearchBox);
                        }
                    }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to toggle desktop mode via Windows key: {ex.Message}");
            }
        }
        
        // Context Menu Event Handlers
        private void ContextMenu_Open(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && 
                menuItem.Parent is ContextMenu contextMenu && 
                contextMenu.Tag is WindowInfo window)
            {
                try
                {
                    if (window.IsMinimized)
                    {
                        NativeMethods.ShowWindow(window.Handle, NativeMethods.SW_RESTORE);
                    }
                    NativeMethods.SetForegroundWindow(window.Handle);
                    NativeMethods.ShowWindow(window.Handle, NativeMethods.SW_RESTORE);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to open window: {ex.Message}");
                }
            }
        }
        
        private void ContextMenu_Close(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && 
                menuItem.Parent is ContextMenu contextMenu && 
                contextMenu.Tag is WindowInfo window)
            {
                try
                {
                    // Send close message to window
                    const int WM_CLOSE = 0x0010;
                    NativeMethods.SendMessage(window.Handle, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to close window: {ex.Message}");
                }
            }
        }
        
        private void ContextMenu_UnpinFromDock(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && 
                menuItem.Parent is ContextMenu contextMenu && 
                contextMenu.Tag is WindowInfo window)
            {
                try
                {
                    // Remove from dock by removing from running applications
                    if (_viewModel != null)
                    {
                        _viewModel.RemoveFromDock(window);
                        System.Diagnostics.Debug.WriteLine($"Unpinned {window.Title} from dock");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to unpin from dock: {ex.Message}");
                }
            }
        }
        
        private void ContextMenu_OpenFileLocation(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && 
                menuItem.Parent is ContextMenu contextMenu && 
                contextMenu.Tag is WindowInfo window)
            {
                try
                {
                    // Get the process path
                    var process = Process.GetProcessById((int)window.ProcessId);
                    if (process.MainModule?.FileName != null)
                    {
                        var filePath = process.MainModule.FileName;
                        var folderPath = Path.GetDirectoryName(filePath);
                        
                        if (!string.IsNullOrEmpty(folderPath) && Directory.Exists(folderPath))
                        {
                            // Open explorer and select the file
                            Process.Start("explorer.exe", $"/select,\"{filePath}\"");
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to open file location: {ex.Message}");
                }
            }
        }
        
        private void ContextMenu_Properties(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && 
                menuItem.Parent is ContextMenu contextMenu && 
                contextMenu.Tag is WindowInfo window)
            {
                try
                {
                    // Get the process path
                    var process = Process.GetProcessById((int)window.ProcessId);
                    if (process.MainModule?.FileName != null)
                    {
                        var filePath = process.MainModule.FileName;
                        
                        // Show properties dialog using shell execute
                        var processInfo = new ProcessStartInfo
                        {
                            FileName = "rundll32.exe",
                            Arguments = $"shell32.dll,Properties_RunDLL \"{filePath}\"",
                            UseShellExecute = true
                        };
                        Process.Start(processInfo);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to show properties: {ex.Message}");
                }
            }
        }
        
        #region DockManager Integration Methods
        
        /// <summary>
        /// Updates the dock orientation based on position.
        /// </summary>
        /// <param name="position">The new dock position.</param>
        public void UpdateOrientation(Services.DockPosition position)
        {
            try
            {
                // Update dock layout based on position
                // Find the StackPanel that contains the running apps
                var runningAppsControl = FindName("RunningAppsControl") as ItemsControl;
                if (runningAppsControl?.ItemsPanel != null)
                {
                    // Create new panel template based on position
                    var panelTemplate = new ItemsPanelTemplate();
                    FrameworkElementFactory stackPanelFactory;
                    
                    switch (position)
                    {
                        case Services.DockPosition.Bottom:
                            // Horizontal layout for bottom dock
                            stackPanelFactory = new FrameworkElementFactory(typeof(StackPanel));
                            stackPanelFactory.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
                            break;
                            
                        case Services.DockPosition.Left:
                        case Services.DockPosition.Right:
                            // Vertical layout for side docks
                            stackPanelFactory = new FrameworkElementFactory(typeof(StackPanel));
                            stackPanelFactory.SetValue(StackPanel.OrientationProperty, Orientation.Vertical);
                            break;
                            
                        default:
                            stackPanelFactory = new FrameworkElementFactory(typeof(StackPanel));
                            stackPanelFactory.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
                            break;
                    }
                    
                    panelTemplate.VisualTree = stackPanelFactory;
                    runningAppsControl.ItemsPanel = panelTemplate;
                }
                
                System.Diagnostics.Debug.WriteLine($"Dock orientation updated to: {position}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating dock orientation: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Sets the auto-hide behavior of the dock.
        /// </summary>
        /// <param name="autoHide">Whether to enable auto-hide.</param>
        public void SetAutoHide(bool autoHide)
        {
            try
            {
                // Implementation depends on auto-hide mechanism
                // This would trigger the auto-hide behavior
                System.Diagnostics.Debug.WriteLine($"Dock auto-hide set to: {autoHide}");
                
                // TODO: Implement actual auto-hide logic based on dock design
                // This might involve mouse enter/leave events and animation
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting auto-hide: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Sets the icon size for dock items.
        /// </summary>
        /// <param name="iconSize">The new icon size in pixels.</param>
        public void SetIconSize(int iconSize)
        {
            try
            {
                // Update all dock icons to new size
                var buttons = FindVisualChildren<Button>(this);
                foreach (var button in buttons)
                {
                    button.Width = iconSize;
                    button.Height = iconSize;
                    
                    // Update image inside button if exists
                    var image = FindVisualChild<System.Windows.Controls.Image>(button);
                    if (image != null)
                    {
                        image.Width = iconSize - 8; // Slightly smaller than button for padding
                        image.Height = iconSize - 8;
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"Dock icon size set to: {iconSize}px");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting icon size: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Sets the icon scale multiplier for dock items.
        /// </summary>
        /// <param name="iconScale">The scale multiplier (0.5 to 2.0).</param>
        public void SetIconScale(double iconScale)
        {
            try
            {
                // Apply scale transform to all dock icons
                var buttons = FindVisualChildren<Button>(this);
                foreach (var button in buttons)
                {
                    if (button.RenderTransform == null)
                    {
                        button.RenderTransform = new System.Windows.Media.ScaleTransform();
                        button.RenderTransformOrigin = new Point(0.5, 0.5);
                    }
                    
                    if (button.RenderTransform is System.Windows.Media.ScaleTransform scaleTransform)
                    {
                        scaleTransform.ScaleX = iconScale;
                        scaleTransform.ScaleY = iconScale;
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"Dock icon scale set to: {iconScale}x");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting icon scale: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Sets the spacing between dock icons.
        /// </summary>
        /// <param name="iconSpacing">The spacing in pixels.</param>
        public void SetIconSpacing(int iconSpacing)
        {
            try
            {
                // Update margin for all dock items
                var buttons = FindVisualChildren<Button>(this);
                foreach (var button in buttons)
                {
                    button.Margin = new Thickness(iconSpacing / 2, 0, iconSpacing / 2, 0);
                }
                
                System.Diagnostics.Debug.WriteLine($"Dock icon spacing set to: {iconSpacing}px");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting icon spacing: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Sets whether to show recent applications in the dock.
        /// </summary>
        /// <param name="showRecentApps">Whether to show recent applications.</param>
        public void SetShowRecentApps(bool showRecentApps)
        {
            try
            {
                if (_viewModel != null)
                {
                    _viewModel.ShowRecentApps = showRecentApps;
                }
                
                System.Diagnostics.Debug.WriteLine($"Show recent apps set to: {showRecentApps}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting show recent apps: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Helper method to find visual children of a specific type.
        /// </summary>
        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    var child = System.Windows.Media.VisualTreeHelper.GetChild(depObj, i);
                    if (child is T t)
                    {
                        yield return t;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }
        
        /// <summary>
        /// Helper method to find a visual child of a specific type.
        /// </summary>
        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                    return result;
                
                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                    return childOfChild;
            }
            
            return null;
        }
        
        #endregion
    }
}
