using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using SeroDesk.Platform;
using SeroDesk.ViewModels;

namespace SeroDesk.Views
{
    public partial class SeroControlCenter : UserControl
    {
        private ControlCenterViewModel? _viewModel;
        private bool _isVisible = false;
        
        public SeroControlCenter()
        {
            InitializeComponent();
            
            Loaded += (s, e) =>
            {
                _viewModel = DataContext as ControlCenterViewModel;
                if (_viewModel == null)
                {
                    _viewModel = new ControlCenterViewModel();
                    DataContext = _viewModel;
                }
                
                UpdateToggleStates();
            };
            
            // Add click-outside handler to auto-hide
            this.MouseDown += OnMouseDown;
        }
        
        public void Show()
        {
            if (_isVisible) return;
            
            _isVisible = true;
            ControlPanel.Visibility = Visibility.Visible;
            
            // Subscribe to global mouse events to detect clicks outside
            if (Application.Current.MainWindow != null)
            {
                Application.Current.MainWindow.PreviewMouseDown += MainWindow_PreviewMouseDown;
            }
            
            // Slide down animation
            var slideDown = new DoubleAnimation
            {
                From = -1080,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(400),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            
            ControlTransform.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, slideDown);
        }
        
        public void Hide()
        {
            if (!_isVisible) return;
            
            _isVisible = false;
            
            // Unsubscribe from global mouse events
            if (Application.Current.MainWindow != null)
            {
                Application.Current.MainWindow.PreviewMouseDown -= MainWindow_PreviewMouseDown;
            }
            
            // Slide up animation
            var slideUp = new DoubleAnimation
            {
                From = 0,
                To = -1080,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            
            slideUp.Completed += (s, e) =>
            {
                ControlPanel.Visibility = Visibility.Collapsed;
            };
            
            ControlTransform.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, slideUp);
        }
        
        public new bool IsVisible => _isVisible;
        
        private void WiFiToggle_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _viewModel?.ToggleWiFi();
                UpdateToggleState(WiFiToggle, _viewModel?.IsWiFiEnabled ?? false);
                AnimateToggleFeedback(WiFiToggle);
            }
            catch (Exception ex)
            {
                Services.Logger.Error("WiFi toggle failed", ex);
                ShowToggleError(WiFiToggle);
            }
        }

        private void BluetoothToggle_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _viewModel?.ToggleBluetooth();
                UpdateToggleState(BluetoothToggle, _viewModel?.IsBluetoothEnabled ?? false);
                AnimateToggleFeedback(BluetoothToggle);
            }
            catch (Exception ex)
            {
                Services.Logger.Error("Bluetooth toggle failed", ex);
                ShowToggleError(BluetoothToggle);
            }
        }
        
        private void AirplaneModeToggle_Click(object sender, RoutedEventArgs e)
        {
            _viewModel?.ToggleAirplaneMode();
            UpdateToggleState(AirplaneModeToggle, _viewModel?.IsAirplaneModeEnabled ?? false);
        }
        
        private void HotspotToggle_Click(object sender, RoutedEventArgs e)
        {
            _viewModel?.ToggleHotspot();
            UpdateToggleState(HotspotToggle, _viewModel?.IsHotspotEnabled ?? false);
        }
        
        private void OrientationLockToggle_Click(object sender, RoutedEventArgs e)
        {
            _viewModel?.ToggleOrientationLock();
            var isLocked = _viewModel?.IsOrientationLocked ?? false;

            // Find OrientationLockStatus inside the ControlTemplate
            var statusBlock = FindNameInTemplate<System.Windows.Controls.TextBlock>(OrientationLockToggle, "OrientationLockStatus");
            if (statusBlock != null)
            {
                statusBlock.Text = isLocked ? "On" : "Off";
            }
        }

        private T? FindNameInTemplate<T>(DependencyObject parent, string name) where T : FrameworkElement
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T element && element.Name == name)
                    return element;
                var result = FindNameInTemplate<T>(child, name);
                if (result != null) return result;
            }
            return null;
        }
        
        private void BrightnessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_viewModel != null)
            {
                _viewModel.BrightnessLevel = (int)e.NewValue;
                _viewModel.SetBrightness((int)e.NewValue);
            }
        }
        
        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_viewModel != null)
            {
                _viewModel.VolumeLevel = (int)e.NewValue;
                _viewModel.SetVolume((int)e.NewValue);
            }
        }
        
        private void UpdateToggleStates()
        {
            if (_viewModel == null) return;
            
            UpdateToggleState(WiFiToggle, _viewModel.IsWiFiEnabled);
            UpdateToggleState(BluetoothToggle, _viewModel.IsBluetoothEnabled);
            UpdateToggleState(AirplaneModeToggle, _viewModel.IsAirplaneModeEnabled);
            UpdateToggleState(HotspotToggle, _viewModel.IsHotspotEnabled);
            
            if (BrightnessSlider != null)
                BrightnessSlider.Value = _viewModel.BrightnessLevel;
            if (VolumeSlider != null)
                VolumeSlider.Value = _viewModel.VolumeLevel;
        }
        
        private void UpdateToggleState(Button button, bool isEnabled)
        {
            button.Background = isEnabled ?
                new SolidColorBrush(Color.FromRgb(0, 122, 255)) : // iOS blue
                new SolidColorBrush(Color.FromRgb(58, 58, 60));   // #3A3A3C
        }

        /// <summary>
        /// Plays a subtle scale animation on a toggle button for tactile feedback.
        /// </summary>
        private void AnimateToggleFeedback(Button button)
        {
            var scaleTransform = new System.Windows.Media.ScaleTransform(1, 1);
            button.RenderTransform = scaleTransform;
            button.RenderTransformOrigin = new Point(0.5, 0.5);

            var scaleDown = new DoubleAnimation(0.92, TimeSpan.FromMilliseconds(80))
            {
                AutoReverse = true,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            scaleTransform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, scaleDown);
            scaleTransform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, scaleDown);
        }

        /// <summary>
        /// Shows a brief red flash on a toggle button to indicate a failed operation.
        /// </summary>
        private void ShowToggleError(Button button)
        {
            var errorBrush = new SolidColorBrush(Color.FromRgb(255, 59, 48)); // iOS red
            var originalBg = button.Background;
            button.Background = errorBrush;

            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(800)
            };
            timer.Tick += (s, e) =>
            {
                button.Background = originalBg;
                timer.Stop();
            };
            timer.Start();
        }
        
        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            // Prevent hiding when clicking inside the control center
            e.Handled = true;
        }
        
        private void MainWindow_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // Check if click is outside this control
            var position = e.GetPosition(this);
            var bounds = new Rect(0, 0, ActualWidth, ActualHeight);
            
            if (!bounds.Contains(position) && _isVisible)
            {
                Hide();
            }
        }
    }
}