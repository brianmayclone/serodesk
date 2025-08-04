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
            _viewModel?.ToggleWiFi();
            UpdateToggleState(WiFiToggle, _viewModel?.IsWiFiEnabled ?? false);
        }
        
        private void BluetoothToggle_Click(object sender, RoutedEventArgs e)
        {
            _viewModel?.ToggleBluetooth();
            UpdateToggleState(BluetoothToggle, _viewModel?.IsBluetoothEnabled ?? false);
        }
        
        private void MobileDataToggle_Click(object sender, RoutedEventArgs e)
        {
            _viewModel?.ToggleMobileData();
            UpdateToggleState(MobileDataToggle, _viewModel?.IsMobileDataEnabled ?? false);
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
            UpdateToggleState(OrientationLockToggle, isLocked);
            OrientationLockStatus.Text = isLocked ? "On" : "Off";
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
            UpdateToggleState(MobileDataToggle, _viewModel.IsMobileDataEnabled);
            UpdateToggleState(AirplaneModeToggle, _viewModel.IsAirplaneModeEnabled);
            UpdateToggleState(HotspotToggle, _viewModel.IsHotspotEnabled);
            UpdateToggleState(OrientationLockToggle, _viewModel.IsOrientationLocked);
            
            if (BrightnessSlider != null)
                BrightnessSlider.Value = _viewModel.BrightnessLevel;
            if (VolumeSlider != null)
                VolumeSlider.Value = _viewModel.VolumeLevel;
        }
        
        private void UpdateToggleState(Button button, bool isEnabled)
        {
            button.Background = isEnabled ? 
                new SolidColorBrush(Color.FromRgb(0, 122, 255)) : // iOS blue
                new SolidColorBrush(Color.FromRgb(48, 48, 48));   // Dark gray
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