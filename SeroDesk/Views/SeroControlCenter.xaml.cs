using System.Windows;
using System.Windows.Controls;
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
        }
        
        public void Show()
        {
            if (_isVisible) return;
            
            _isVisible = true;
            ControlPanel.Visibility = Visibility.Visible;
            
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
            
            OrientationLockStatus.Text = _viewModel.IsOrientationLocked ? "On" : "Off";
        }
        
        private void UpdateToggleState(Button button, bool isEnabled)
        {
            button.Background = new SolidColorBrush(isEnabled ? 
                Color.FromRgb(0, 128, 255) :  // Blue for enabled
                Color.FromRgb(102, 102, 102)); // Gray for disabled
        }
    }
}