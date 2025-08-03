using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using SeroDesk.Platform;
using SeroDesk.ViewModels;

namespace SeroDesk.Views
{
    public partial class SeroDock : UserControl
    {
        private DockViewModel? _viewModel;
        
        public SeroDock()
        {
            InitializeComponent();
            
            Loaded += (s, e) =>
            {
                _viewModel = DataContext as DockViewModel;
            };
        }
        
        public void Initialize()
        {
            _viewModel = new DockViewModel();
            DataContext = _viewModel;
            
            _viewModel.StartMonitoringWindows();
            AnimateIn();
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