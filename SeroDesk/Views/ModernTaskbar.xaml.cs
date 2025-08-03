using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using SeroDesk.Models;
using SeroDesk.Platform;
using SeroDesk.ViewModels;

namespace SeroDesk.Views
{
    public partial class ModernTaskbar : UserControl
    {
        private TaskbarViewModel? _viewModel;
        private DispatcherTimer _clockTimer;
        private IntPtr _currentThumbnail = IntPtr.Zero;
        
        public ModernTaskbar()
        {
            InitializeComponent();
            
            _clockTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _clockTimer.Tick += UpdateClock;
            _clockTimer.Start();
            
            UpdateClock(null, null);
        }
        
        public void Initialize()
        {
            _viewModel = new TaskbarViewModel();
            DataContext = _viewModel;
            
            _viewModel.LoadQuickLaunchApps();
            _viewModel.StartMonitoringWindows();
        }
        
        private void UpdateClock(object? sender, EventArgs? e)
        {
            ClockText.Text = DateTime.Now.ToString("h:mm tt\nM/d/yyyy");
        }
        
        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            // Show start menu (to be implemented)
            ShowStartMenu();
        }
        
        private void QuickLaunchButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is AppIcon app)
            {
                app.Launch();
            }
        }
        
        private void TaskbarButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is TaskbarButton button && button.AppWindow != null)
            {
                if (button.AppWindow.IsMinimized)
                {
                    WindowManager.Instance.RestoreWindow(button.AppWindow.Handle);
                }
                else
                {
                    NativeMethods.ShowWindow(button.AppWindow.Handle, NativeMethods.SW_MINIMIZE);
                }
            }
        }
        
        private void TaskbarButton_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is TaskbarButton button && button.AppWindow != null)
            {
                ShowWindowPreview(button);
            }
        }
        
        private void TaskbarButton_MouseLeave(object sender, MouseEventArgs e)
        {
            HideWindowPreview();
        }
        
        private void ShowWindowPreview(TaskbarButton button)
        {
            try
            {
                // Position popup above button
                PreviewPopup.PlacementTarget = button;
                
                // Create DWM thumbnail
                var hostHandle = new System.Windows.Interop.WindowInteropHelper(
                    Window.GetWindow(this)).Handle;
                
                _currentThumbnail = WindowManager.Instance.CreateWindowThumbnail(
                    button.AppWindow.Handle, 
                    hostHandle,
                    new Rect(0, 0, 300, 200));
                
                if (_currentThumbnail != IntPtr.Zero)
                {
                    PreviewImage.Visibility = Visibility.Collapsed;
                    ThumbnailHost.Visibility = Visibility.Visible;
                }
                else
                {
                    // Fallback to static image
                    if (button.AppWindow.Icon != null)
                    {
                        PreviewImage.Source = SeroDesk.Core.Extensions.ToBitmapSource(
                            SeroDesk.Core.Extensions.ToBitmap(button.AppWindow.Icon) ?? new System.Drawing.Bitmap(1, 1));
                    }
                    PreviewImage.Visibility = Visibility.Visible;
                    ThumbnailHost.Visibility = Visibility.Collapsed;
                }
                
                PreviewPopup.IsOpen = true;
            }
            catch { }
        }
        
        private void HideWindowPreview()
        {
            PreviewPopup.IsOpen = false;
            
            if (_currentThumbnail != IntPtr.Zero)
            {
                WindowManager.Instance.DestroyWindowThumbnail(_currentThumbnail);
                _currentThumbnail = IntPtr.Zero;
            }
        }
        
        private void ShowStartMenu()
        {
            // Create start menu popup
            var startMenu = new StartMenuPopup
            {
                PlacementTarget = StartButton,
                Placement = System.Windows.Controls.Primitives.PlacementMode.Top,
                IsOpen = true
            };
        }
        
        public void AnimateShow()
        {
            var slideIn = new DoubleAnimation
            {
                From = ActualHeight,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            
            var transform = new TranslateTransform();
            RenderTransform = transform;
            transform.BeginAnimation(TranslateTransform.YProperty, slideIn);
        }
        
        public void AnimateHide()
        {
            var slideOut = new DoubleAnimation
            {
                From = 0,
                To = ActualHeight,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            
            var transform = RenderTransform as TranslateTransform ?? new TranslateTransform();
            RenderTransform = transform;
            transform.BeginAnimation(TranslateTransform.YProperty, slideOut);
        }
    }
    
    // TaskbarButton control
    public class TaskbarButton : Button
    {
        public static readonly DependencyProperty AppWindowProperty =
            DependencyProperty.Register("AppWindow", typeof(WindowInfo), typeof(TaskbarButton));
        
        public WindowInfo AppWindow
        {
            get => (WindowInfo)GetValue(AppWindowProperty);
            set => SetValue(AppWindowProperty, value);
        }
        
        static TaskbarButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TaskbarButton),
                new FrameworkPropertyMetadata(typeof(TaskbarButton)));
        }
    }
    
    // Start Menu Popup
    public class StartMenuPopup : System.Windows.Controls.Primitives.Popup
    {
        public StartMenuPopup()
        {
            AllowsTransparency = true;
            PopupAnimation = System.Windows.Controls.Primitives.PopupAnimation.Fade;
            StaysOpen = false;
            
            var border = new Border
            {
                Background = Application.Current.FindResource("SurfaceBrush") as System.Windows.Media.Brush,
                BorderBrush = Application.Current.FindResource("SubtleBrush") as System.Windows.Media.Brush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Width = 400,
                Height = 600,
                Margin = new Thickness(10)
            };
            
            border.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 10,
                Opacity = 0.3
            };
            
            // Add content
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(60) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            
            // Header
            var header = new TextBlock
            {
                Text = "Start",
                FontSize = 24,
                Foreground = Application.Current.FindResource("ForegroundBrush") as System.Windows.Media.Brush,
                Margin = new Thickness(20),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetRow(header, 0);
            grid.Children.Add(header);
            
            // Apps list placeholder
            var appsList = new TextBlock
            {
                Text = "All Apps",
                Foreground = Application.Current.FindResource("SubtleBrush") as System.Windows.Media.Brush,
                Margin = new Thickness(20),
                VerticalAlignment = VerticalAlignment.Top
            };
            Grid.SetRow(appsList, 1);
            grid.Children.Add(appsList);
            
            border.Child = grid;
            Child = border;
        }
    }
}