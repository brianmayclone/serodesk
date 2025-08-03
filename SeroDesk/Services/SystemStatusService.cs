using System.ComponentModel;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace SeroDesk.Services
{
    public class SystemStatusService : INotifyPropertyChanged
    {
        private static SystemStatusService? _instance;
        public static SystemStatusService Instance => _instance ?? (_instance = new SystemStatusService());
        
        private readonly DispatcherTimer _updateTimer;
        private string _currentTime = string.Empty;
        private int _batteryPercentage = 100;
        private bool _isCharging = false;
        private string _networkStatus = "WiFi";
        private int _signalStrength = 4;
        
        public string CurrentTime
        {
            get => _currentTime;
            private set { _currentTime = value; OnPropertyChanged(); }
        }
        
        public int BatteryPercentage
        {
            get => _batteryPercentage;
            private set { _batteryPercentage = value; OnPropertyChanged(); }
        }
        
        public bool IsCharging
        {
            get => _isCharging;
            private set { _isCharging = value; OnPropertyChanged(); }
        }
        
        public string NetworkStatus
        {
            get => _networkStatus;
            private set { _networkStatus = value; OnPropertyChanged(); }
        }
        
        public int SignalStrength
        {
            get => _signalStrength;
            private set { _signalStrength = value; OnPropertyChanged(); }
        }
        
        private SystemStatusService()
        {
            _updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _updateTimer.Tick += UpdateStatus;
            _updateTimer.Start();
            
            UpdateStatus(null, EventArgs.Empty);
        }
        
        private void UpdateStatus(object? sender, EventArgs e)
        {
            UpdateTime();
            UpdateBatteryStatus();
            UpdateNetworkStatus();
        }
        
        private void UpdateTime()
        {
            CurrentTime = DateTime.Now.ToString("HH:mm");
        }
        
        private void UpdateBatteryStatus()
        {
            try
            {
                var batteryStatus = GetBatteryStatus();
                BatteryPercentage = batteryStatus.percentage;
                IsCharging = batteryStatus.isCharging;
            }
            catch
            {
                // Fallback for systems without battery
                BatteryPercentage = 100;
                IsCharging = false;
            }
        }
        
        private void UpdateNetworkStatus()
        {
            try
            {
                if (NetworkInterface.GetIsNetworkAvailable())
                {
                    var activeInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                        .Where(ni => ni.OperationalStatus == OperationalStatus.Up && 
                                   ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                        .ToList();
                    
                    if (activeInterfaces.Any(ni => ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211))
                    {
                        NetworkStatus = "WiFi";
                        SignalStrength = GetWiFiSignalStrength();
                    }
                    else if (activeInterfaces.Any(ni => ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet))
                    {
                        NetworkStatus = "Ethernet";
                        SignalStrength = 4; // Ethernet always full strength
                    }
                    else
                    {
                        NetworkStatus = "Connected";
                        SignalStrength = 3;
                    }
                }
                else
                {
                    NetworkStatus = "No Internet";
                    SignalStrength = 0;
                }
            }
            catch
            {
                NetworkStatus = "Unknown";
                SignalStrength = 0;
            }
        }
        
        private (int percentage, bool isCharging) GetBatteryStatus()
        {
            var status = GetSystemPowerStatus();
            if (status.BatteryFlag == 128) // No battery
            {
                return (100, false);
            }
            
            return (status.BatteryLifePercent, status.ACLineStatus == 1);
        }
        
        private int GetWiFiSignalStrength()
        {
            try
            {
                // Simplified signal strength based on network availability
                // In a real implementation, you would use native WiFi APIs
                var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(ni => ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 && 
                               ni.OperationalStatus == OperationalStatus.Up)
                    .ToList();
                
                if (interfaces.Any())
                {
                    // Simulate signal strength based on connection quality
                    return 4; // Good signal for demo
                }
                
                return 2; // Weak signal
            }
            catch
            {
                return 3;
            }
        }
        
        [StructLayout(LayoutKind.Sequential)]
        private struct SystemPowerStatus
        {
            public byte ACLineStatus;
            public byte BatteryFlag;
            public byte BatteryLifePercent;
            public byte Reserved1;
            public uint BatteryLifeTime;
            public uint BatteryFullLifeTime;
        }
        
        [DllImport("kernel32.dll")]
        private static extern bool GetSystemPowerStatus(out SystemPowerStatus sps);
        
        private SystemPowerStatus GetSystemPowerStatus()
        {
            GetSystemPowerStatus(out var status);
            return status;
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}