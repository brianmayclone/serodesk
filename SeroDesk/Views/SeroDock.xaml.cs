using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Text;
using System.Collections.Generic;
using SeroDesk.Platform;
using SeroDesk.ViewModels;

namespace SeroDesk.Views
{
    public partial class SeroDock : UserControl
    {
        private DockViewModel? _viewModel;
        private List<IntPtr> _minimizedWindows = new List<IntPtr>();
        private bool _isDesktopMode = false;
        
        public SeroDock()
        {
            InitializeComponent();
            
            Loaded += (s, e) =>
            {
                _viewModel = DataContext as DockViewModel;
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
                LoadSystemIcons();
            }), System.Windows.Threading.DispatcherPriority.Loaded);
            
            AnimateIn();
        }
        
        private void LoadSystemIcons()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("LoadSystemIcons: Starting to load system icons");
                
                // Load Explorer icon - Try multiple approaches
                var explorerImage = this.FindName("ExplorerIcon") as System.Windows.Controls.Image;
                if (explorerImage != null)
                {
                    System.Diagnostics.Debug.WriteLine("ExplorerIcon image found");
                    
                    // Try Computer icon first
                    var explorerIconSource = IconExtractor.GetSystemIcon(Platform.SystemIconType.Computer);
                    if (explorerIconSource != null)
                    {
                        explorerImage.Source = explorerIconSource;
                        System.Diagnostics.Debug.WriteLine("Explorer icon loaded from Computer");
                    }
                    else
                    {
                        // Fallback: Try loading directly from explorer.exe
                        explorerIconSource = IconExtractor.GetIconForFile(@"C:\Windows\explorer.exe", true);
                        if (explorerIconSource != null)
                        {
                            explorerImage.Source = explorerIconSource;
                            System.Diagnostics.Debug.WriteLine("Explorer icon loaded from explorer.exe");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("Failed to load Explorer icon");
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("ExplorerIcon image not found in XAML");
                }
                
                // Load Recycle Bin icon
                var recycleBinImage = this.FindName("RecycleBinIcon") as System.Windows.Controls.Image;
                if (recycleBinImage != null)
                {
                    System.Diagnostics.Debug.WriteLine("RecycleBinIcon image found");
                    
                    var recycleBinIconSource = IconExtractor.GetSystemIcon(Platform.SystemIconType.RecycleBin);
                    if (recycleBinIconSource != null)
                    {
                        recycleBinImage.Source = recycleBinIconSource;
                        System.Diagnostics.Debug.WriteLine("Recycle Bin icon loaded");
                    }
                    else
                    {
                        // Fallback: Try loading from shell32.dll
                        recycleBinIconSource = IconExtractor.GetIconForFile(@"C:\Windows\System32\shell32.dll", true);
                        if (recycleBinIconSource != null)
                        {
                            recycleBinImage.Source = recycleBinIconSource;
                            System.Diagnostics.Debug.WriteLine("Recycle Bin icon loaded from shell32.dll");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("Failed to load Recycle Bin icon");
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("RecycleBinIcon image not found in XAML");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception in LoadSystemIcons: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
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
            
            var bounceStoryboard = new Storyboard();
            
            var scaleUpX = new DoubleAnimation
            {
                To = 1.3,
                Duration = TimeSpan.FromMilliseconds(100),
                AutoReverse = true,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            
            var scaleUpY = new DoubleAnimation
            {
                To = 1.3,
                Duration = TimeSpan.FromMilliseconds(100),
                AutoReverse = true,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            
            Storyboard.SetTarget(scaleUpX, button);
            Storyboard.SetTarget(scaleUpY, button);
            Storyboard.SetTargetProperty(scaleUpX, new PropertyPath("RenderTransform.ScaleX"));
            Storyboard.SetTargetProperty(scaleUpY, new PropertyPath("RenderTransform.ScaleY"));
            
            // Ensure button has a ScaleTransform
            if (button.RenderTransform == null)
            {
                button.RenderTransform = new System.Windows.Media.ScaleTransform();
                button.RenderTransformOrigin = new Point(0.5, 0.5);
            }
            
            bounceStoryboard.Children.Add(scaleUpX);
            bounceStoryboard.Children.Add(scaleUpY);
            bounceStoryboard.Begin();
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
    }
}