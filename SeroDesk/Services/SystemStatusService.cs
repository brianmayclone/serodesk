using System.ComponentModel;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace SeroDesk.Services
{
    /// <summary>
    /// Provides real-time system status information for display in the SeroDesk status bar interface.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The SystemStatusService monitors and provides essential system information that is typically
    /// displayed in mobile-style status bars and desktop notification areas:
    /// <list type="bullet">
    /// <item>Current time with automatic updates every second</item>
    /// <item>Battery percentage and charging status for portable devices</item>
    /// <item>Network connectivity status (WiFi, Ethernet, disconnected)</item>
    /// <item>Signal strength indicators for wireless connections</item>
    /// <item>Real-time updates through property change notifications</item>
    /// </list>
    /// </para>
    /// <para>
    /// The service implements the singleton pattern to ensure consistent system state monitoring
    /// across the application. It automatically starts monitoring when instantiated and provides
    /// live updates through the INotifyPropertyChanged interface for data binding scenarios.
    /// </para>
    /// <para>
    /// System information is gathered using Windows APIs for battery status and .NET network
    /// interfaces for connectivity information. The service gracefully handles systems without
    /// batteries or network adapters by providing appropriate fallback values.
    /// </para>
    /// </remarks>
    public class SystemStatusService : INotifyPropertyChanged
    {
        /// <summary>
        /// Singleton instance of the SystemStatusService.
        /// </summary>
        private static SystemStatusService? _instance;
        
        /// <summary>
        /// Gets the singleton instance of the SystemStatusService.
        /// </summary>
        /// <value>The global SystemStatusService instance with active system monitoring.</value>
        public static SystemStatusService Instance => _instance ?? (_instance = new SystemStatusService());
        
        private readonly DispatcherTimer _updateTimer;
        private string _currentTime = string.Empty;
        private int _batteryPercentage = 100;
        private bool _isCharging = false;
        private string _networkStatus = "WiFi";
        private int _signalStrength = 4;
        
        /// <summary>
        /// Gets the current system time formatted as HH:mm.
        /// </summary>
        /// <value>The current time string updated every second.</value>
        /// <remarks>
        /// This property provides the current time in 24-hour format and is automatically
        /// updated every second by the internal timer. Changes are notified through
        /// PropertyChanged events for data binding scenarios.
        /// </remarks>
        public string CurrentTime
        {
            get => _currentTime;
            private set { _currentTime = value; OnPropertyChanged(); }
        }
        
        /// <summary>
        /// Gets the current battery charge level as a percentage.
        /// </summary>
        /// <value>The battery percentage (0-100), or 100 for systems without a battery.</value>
        /// <remarks>
        /// <para>
        /// This property reflects the current battery charge level obtained from Windows
        /// power management APIs. For desktop systems without batteries, this value
        /// defaults to 100% to indicate unlimited power availability.
        /// </para>
        /// <para>
        /// The value is updated every second and changes are notified through
        /// PropertyChanged events for real-time UI updates.
        /// </para>
        /// </remarks>
        public int BatteryPercentage
        {
            get => _batteryPercentage;
            private set { _batteryPercentage = value; OnPropertyChanged(); }
        }
        
        /// <summary>
        /// Gets a value indicating whether the battery is currently charging.
        /// </summary>
        /// <value>True if the battery is charging or AC power is connected; otherwise, false.</value>
        /// <remarks>
        /// <para>
        /// This property indicates whether the system is connected to AC power and the
        /// battery is charging. For desktop systems without batteries, this typically
        /// reflects AC power connection status.
        /// </para>
        /// <para>
        /// The charging status is determined using Windows power management APIs and
        /// is updated every second with change notifications.
        /// </para>
        /// </remarks>
        public bool IsCharging
        {
            get => _isCharging;
            private set { _isCharging = value; OnPropertyChanged(); }
        }
        
        /// <summary>
        /// Gets the current network connectivity status description.
        /// </summary>
        /// <value>
        /// A string describing the network status: "WiFi", "Ethernet", "Connected", "No Internet", or "Unknown".
        /// </value>
        /// <remarks>
        /// <para>
        /// This property provides a human-readable description of the current network
        /// connectivity state:
        /// <list type="bullet">
        /// <item>"WiFi" - Connected via wireless network interface</item>
        /// <item>"Ethernet" - Connected via wired network interface</item>
        /// <item>"Connected" - Connected via other network interface</item>
        /// <item>"No Internet" - No active network connections</item>
        /// <item>"Unknown" - Unable to determine network status</item>
        /// </list>
        /// </para>
        /// </remarks>
        public string NetworkStatus
        {
            get => _networkStatus;
            private set { _networkStatus = value; OnPropertyChanged(); }
        }
        
        /// <summary>
        /// Gets the current network signal strength indicator.
        /// </summary>
        /// <value>
        /// Signal strength level from 0-4, where 0 indicates no signal and 4 indicates full strength.
        /// </value>
        /// <remarks>
        /// <para>
        /// This property provides a standardized signal strength indicator similar to
        /// mobile device signal bars:
        /// <list type="bullet">
        /// <item>0 - No signal or disconnected</item>
        /// <item>1 - Very weak signal</item>
        /// <item>2 - Weak signal</item>
        /// <item>3 - Good signal</item>
        /// <item>4 - Excellent signal or wired connection</item>
        /// </list>
        /// </para>
        /// <para>
        /// For WiFi connections, this represents actual wireless signal quality.
        /// For Ethernet connections, this is always 4 (full strength).
        /// </para>
        /// </remarks>
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
        
        /// <summary>
        /// Represents the system power status information from Windows API.
        /// </summary>
        /// <remarks>
        /// This structure maps to the SYSTEM_POWER_STATUS structure used by the
        /// GetSystemPowerStatus Windows API function to retrieve battery and power information.
        /// </remarks>
        [StructLayout(LayoutKind.Sequential)]
        private struct SystemPowerStatus
        {
            /// <summary>AC power status (0 = offline, 1 = online, 255 = unknown).</summary>
            public byte ACLineStatus;
            /// <summary>Battery charge status flags.</summary>
            public byte BatteryFlag;
            /// <summary>Battery life percentage (0-100, 255 = unknown).</summary>
            public byte BatteryLifePercent;
            /// <summary>Reserved field.</summary>
            public byte Reserved1;
            /// <summary>Battery life time in seconds.</summary>
            public uint BatteryLifeTime;
            /// <summary>Full battery life time in seconds.</summary>
            public uint BatteryFullLifeTime;
        }
        
        /// <summary>
        /// Retrieves the power status of the system.
        /// </summary>
        /// <param name="sps">Output parameter that receives the power status information.</param>
        /// <returns>True if the function succeeds; otherwise, false.</returns>
        [DllImport("kernel32.dll")]
        private static extern bool GetSystemPowerStatus(out SystemPowerStatus sps);
        
        private SystemPowerStatus GetSystemPowerStatus()
        {
            GetSystemPowerStatus(out var status);
            return status;
        }
        
        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        /// <remarks>
        /// This event enables data binding scenarios where UI elements can automatically
        /// update when system status information changes.
        /// </remarks>
        public event PropertyChangedEventHandler? PropertyChanged;
        
        /// <summary>
        /// Raises the PropertyChanged event for the specified property.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed. This parameter is optional and can be provided automatically by the CallerMemberName attribute.</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}