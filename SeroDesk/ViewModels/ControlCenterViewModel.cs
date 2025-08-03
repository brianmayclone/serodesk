using System.ComponentModel;
using System.Runtime.CompilerServices;
using SeroDesk.Platform;

namespace SeroDesk.ViewModels
{
    public class ControlCenterViewModel : INotifyPropertyChanged
    {
        private bool _isWiFiEnabled;
        private bool _isBluetoothEnabled;
        private bool _isMobileDataEnabled;
        private bool _isAirplaneModeEnabled;
        private bool _isHotspotEnabled;
        private bool _isOrientationLocked;
        private int _brightnessLevel;
        private int _volumeLevel;
        
        public bool IsWiFiEnabled
        {
            get => _isWiFiEnabled;
            set { _isWiFiEnabled = value; OnPropertyChanged(); }
        }
        
        public bool IsBluetoothEnabled
        {
            get => _isBluetoothEnabled;
            set { _isBluetoothEnabled = value; OnPropertyChanged(); }
        }
        
        public bool IsMobileDataEnabled
        {
            get => _isMobileDataEnabled;
            set { _isMobileDataEnabled = value; OnPropertyChanged(); }
        }
        
        public bool IsAirplaneModeEnabled
        {
            get => _isAirplaneModeEnabled;
            set { _isAirplaneModeEnabled = value; OnPropertyChanged(); }
        }
        
        public bool IsHotspotEnabled
        {
            get => _isHotspotEnabled;
            set { _isHotspotEnabled = value; OnPropertyChanged(); }
        }
        
        public bool IsOrientationLocked
        {
            get => _isOrientationLocked;
            set { _isOrientationLocked = value; OnPropertyChanged(); }
        }
        
        public int BrightnessLevel
        {
            get => _brightnessLevel;
            set { _brightnessLevel = value; OnPropertyChanged(); }
        }
        
        public int VolumeLevel
        {
            get => _volumeLevel;
            set { _volumeLevel = value; OnPropertyChanged(); }
        }
        
        public ControlCenterViewModel()
        {
            LoadCurrentSettings();
        }
        
        private void LoadCurrentSettings()
        {
            // Load current system settings
            try
            {
                _isWiFiEnabled = SystemSettings.IsWiFiEnabled();
                _isBluetoothEnabled = SystemSettings.IsBluetoothEnabled();
                _isMobileDataEnabled = SystemSettings.IsMobileDataEnabled();
                _isAirplaneModeEnabled = SystemSettings.IsAirplaneModeEnabled();
                _isHotspotEnabled = SystemSettings.IsHotspotEnabled();
                _isOrientationLocked = SystemSettings.IsOrientationLocked();
                _brightnessLevel = SystemSettings.GetBrightnessLevel();
                _volumeLevel = SystemSettings.GetVolumeLevel();
            }
            catch
            {
                // Set defaults if system calls fail
                _isWiFiEnabled = true;
                _isBluetoothEnabled = false;
                _isMobileDataEnabled = true;
                _isAirplaneModeEnabled = false;
                _isHotspotEnabled = false;
                _isOrientationLocked = false;
                _brightnessLevel = 75;
                _volumeLevel = 50;
            }
            
            // Notify all properties
            OnPropertyChanged(nameof(IsWiFiEnabled));
            OnPropertyChanged(nameof(IsBluetoothEnabled));
            OnPropertyChanged(nameof(IsMobileDataEnabled));
            OnPropertyChanged(nameof(IsAirplaneModeEnabled));
            OnPropertyChanged(nameof(IsHotspotEnabled));
            OnPropertyChanged(nameof(IsOrientationLocked));
            OnPropertyChanged(nameof(BrightnessLevel));
            OnPropertyChanged(nameof(VolumeLevel));
        }
        
        public void ToggleWiFi()
        {
            IsWiFiEnabled = !IsWiFiEnabled;
            SystemSettings.SetWiFiEnabled(IsWiFiEnabled);
        }
        
        public void ToggleBluetooth()
        {
            IsBluetoothEnabled = !IsBluetoothEnabled;
            SystemSettings.SetBluetoothEnabled(IsBluetoothEnabled);
        }
        
        public void ToggleMobileData()
        {
            IsMobileDataEnabled = !IsMobileDataEnabled;
            SystemSettings.SetMobileDataEnabled(IsMobileDataEnabled);
        }
        
        public void ToggleAirplaneMode()
        {
            IsAirplaneModeEnabled = !IsAirplaneModeEnabled;
            SystemSettings.SetAirplaneModeEnabled(IsAirplaneModeEnabled);
            
            // When airplane mode is enabled, disable WiFi, Bluetooth, and Mobile Data
            if (IsAirplaneModeEnabled)
            {
                IsWiFiEnabled = false;
                IsBluetoothEnabled = false;
                IsMobileDataEnabled = false;
            }
        }
        
        public void ToggleHotspot()
        {
            IsHotspotEnabled = !IsHotspotEnabled;
            SystemSettings.SetHotspotEnabled(IsHotspotEnabled);
        }
        
        public void ToggleOrientationLock()
        {
            IsOrientationLocked = !IsOrientationLocked;
            SystemSettings.SetOrientationLocked(IsOrientationLocked);
        }
        
        public void SetBrightness(int level)
        {
            BrightnessLevel = Math.Max(0, Math.Min(100, level));
            SystemSettings.SetBrightnessLevel(BrightnessLevel);
        }
        
        public void SetVolume(int level)
        {
            VolumeLevel = Math.Max(0, Math.Min(100, level));
            SystemSettings.SetVolumeLevel(VolumeLevel);
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}