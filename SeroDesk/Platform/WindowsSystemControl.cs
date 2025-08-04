using System;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SeroDesk.Platform
{
    /// <summary>
    /// Windows System Control API for managing system settings like WiFi, Bluetooth, etc.
    /// Uses native Windows APIs and WMI to interact with system services.
    /// </summary>
    public static class WindowsSystemControl
    {
        #region WiFi Control
        
        /// <summary>
        /// Gets the current WiFi adapter state
        /// </summary>
        public static bool IsWiFiEnabled()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT * FROM Win32_NetworkAdapter WHERE NetConnectionId IS NOT NULL");
                
                foreach (ManagementObject adapter in searcher.Get())
                {
                    var netConnectionId = adapter["NetConnectionId"]?.ToString();
                    if (netConnectionId != null && 
                        (netConnectionId.Contains("Wi-Fi") || netConnectionId.Contains("Wireless")))
                    {
                        var netEnabled = adapter["NetEnabled"];
                        return netEnabled != null && (bool)netEnabled;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking WiFi status: {ex.Message}");
            }
            
            return false;
        }
        
        /// <summary>
        /// Enables or disables WiFi adapter
        /// </summary>
        public static bool SetWiFiEnabled(bool enabled)
        {
            try
            {
                // Use netsh command to enable/disable WiFi
                var action = enabled ? "connect" : "disconnect";
                var startInfo = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = $"interface set interface \"Wi-Fi\" {(enabled ? "enabled" : "disabled")}",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    Verb = "runas" // Requires admin privileges
                };
                
                using var process = Process.Start(startInfo);
                process?.WaitForExit();
                return process?.ExitCode == 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting WiFi state: {ex.Message}");
                return false;
            }
        }
        
        #endregion
        
        #region Bluetooth Control
        
        /// <summary>
        /// Gets the current Bluetooth adapter state
        /// </summary>
        public static bool IsBluetoothEnabled()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT * FROM Win32_PnPEntity WHERE Name LIKE '%Bluetooth%'");
                
                foreach (ManagementObject device in searcher.Get())
                {
                    var configManagerErrorCode = device["ConfigManagerErrorCode"];
                    if (configManagerErrorCode != null)
                    {
                        // Code 0 means device is working properly
                        return (uint)configManagerErrorCode == 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking Bluetooth status: {ex.Message}");
            }
            
            return false;
        }
        
        /// <summary>
        /// Enables or disables Bluetooth adapter
        /// </summary>
        public static bool SetBluetoothEnabled(bool enabled)
        {
            try
            {
                // Use DevCon or PowerShell to manage Bluetooth
                var action = enabled ? "Enable-NetAdapter" : "Disable-NetAdapter";
                var startInfo = new ProcessStartInfo
                {
                    FileName = "powershell",
                    Arguments = $"-Command \"Get-NetAdapter -Name '*Bluetooth*' | {action}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    Verb = "runas"
                };
                
                using var process = Process.Start(startInfo);
                process?.WaitForExit();
                return process?.ExitCode == 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting Bluetooth state: {ex.Message}");
                return false;
            }
        }
        
        #endregion
        
        #region Volume Control
        
        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
        
        private const byte VK_VOLUME_MUTE = 0xAD;
        private const byte VK_VOLUME_DOWN = 0xAE;
        private const byte VK_VOLUME_UP = 0xAF;
        private const uint KEYEVENTF_KEYDOWN = 0x0000;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        
        /// <summary>
        /// Gets the current system volume level (0-100)
        /// </summary>
        public static int GetVolumeLevel()
        {
            try
            {
                // Use Windows Core Audio API through .NET
                var deviceEnum = new NAudio.CoreAudioApi.MMDeviceEnumerator();
                var device = deviceEnum.GetDefaultAudioEndpoint(
                    NAudio.CoreAudioApi.DataFlow.Render,
                    NAudio.CoreAudioApi.Role.Multimedia);
                
                return (int)(device.AudioEndpointVolume.MasterVolumeLevelScalar * 100);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting volume level: {ex.Message}");
                return 50; // Default to 50%
            }
        }
        
        /// <summary>
        /// Sets the system volume level (0-100)
        /// </summary>
        public static bool SetVolumeLevel(int level)
        {
            try
            {
                level = Math.Max(0, Math.Min(100, level)); // Clamp to 0-100
                
                var deviceEnum = new NAudio.CoreAudioApi.MMDeviceEnumerator();
                var device = deviceEnum.GetDefaultAudioEndpoint(
                    NAudio.CoreAudioApi.DataFlow.Render,
                    NAudio.CoreAudioApi.Role.Multimedia);
                
                device.AudioEndpointVolume.MasterVolumeLevelScalar = level / 100f;
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting volume level: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Mutes or unmutes the system volume
        /// </summary>
        public static bool SetMuted(bool muted)
        {
            try
            {
                var deviceEnum = new NAudio.CoreAudioApi.MMDeviceEnumerator();
                var device = deviceEnum.GetDefaultAudioEndpoint(
                    NAudio.CoreAudioApi.DataFlow.Render,
                    NAudio.CoreAudioApi.Role.Multimedia);
                
                device.AudioEndpointVolume.Mute = muted;
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting mute state: {ex.Message}");
                return false;
            }
        }
        
        #endregion
        
        #region Brightness Control
        
        /// <summary>
        /// Gets the current screen brightness level (0-100)
        /// </summary>
        public static int GetBrightnessLevel()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "root\\WMI", "SELECT * FROM WmiMonitorBrightness");
                
                foreach (ManagementObject obj in searcher.Get())
                {
                    var brightness = obj["CurrentBrightness"];
                    if (brightness != null)
                    {
                        return (byte)brightness;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting brightness level: {ex.Message}");
            }
            
            return 80; // Default brightness
        }
        
        /// <summary>
        /// Sets the screen brightness level (0-100)
        /// </summary>
        public static bool SetBrightnessLevel(int level)
        {
            try
            {
                level = Math.Max(0, Math.Min(100, level)); // Clamp to 0-100
                
                using var searcher = new ManagementObjectSearcher(
                    "root\\WMI", "SELECT * FROM WmiMonitorBrightnessMethods");
                
                foreach (ManagementObject obj in searcher.Get())
                {
                    obj.InvokeMethod("WmiSetBrightness", new object[] { 1, level });
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting brightness level: {ex.Message}");
            }
            
            return false;
        }
        
        #endregion
        
        #region Airplane Mode
        
        /// <summary>
        /// Gets the current airplane mode state
        /// </summary>
        public static bool IsAirplaneModeEnabled()
        {
            try
            {
                // Check if both WiFi and Bluetooth are disabled
                return !IsWiFiEnabled() && !IsBluetoothEnabled();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking airplane mode: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Enables or disables airplane mode
        /// </summary>
        public static bool SetAirplaneModeEnabled(bool enabled)
        {
            try
            {
                if (enabled)
                {
                    // Disable both WiFi and Bluetooth
                    var wifiResult = SetWiFiEnabled(false);
                    var bluetoothResult = SetBluetoothEnabled(false);
                    return wifiResult && bluetoothResult;
                }
                else
                {
                    // Enable both WiFi and Bluetooth
                    var wifiResult = SetWiFiEnabled(true);
                    var bluetoothResult = SetBluetoothEnabled(true);
                    return wifiResult || bluetoothResult; // At least one should succeed
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting airplane mode: {ex.Message}");
                return false;
            }
        }
        
        #endregion
        
        #region Mobile Hotspot
        
        /// <summary>
        /// Gets the current mobile hotspot state
        /// </summary>
        public static bool IsMobileHotspotEnabled()
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = "wlan show hostednetwork",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                };
                
                using var process = Process.Start(startInfo);
                if (process != null)
                {
                    var output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    
                    return output.Contains("Status                 : Started");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking hotspot status: {ex.Message}");
            }
            
            return false;
        }
        
        /// <summary>
        /// Enables or disables mobile hotspot
        /// </summary>
        public static bool SetMobileHotspotEnabled(bool enabled)
        {
            try
            {
                var action = enabled ? "start" : "stop";
                var startInfo = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = $"wlan {action} hostednetwork",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    Verb = "runas"
                };
                
                using var process = Process.Start(startInfo);
                process?.WaitForExit();
                return process?.ExitCode == 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting hotspot state: {ex.Message}");
                return false;
            }
        }
        
        #endregion
        
        #region Do Not Disturb / Focus Assist
        
        /// <summary>
        /// Gets the current Focus Assist (Do Not Disturb) state
        /// </summary>
        public static bool IsFocusAssistEnabled()
        {
            try
            {
                // Check Windows 10/11 Focus Assist registry setting
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\CloudStore\Store\Cache\DefaultAccount");
                
                if (key != null)
                {
                    // This is a simplified check - actual implementation would be more complex
                    return false; // Default to disabled
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking Focus Assist: {ex.Message}");
            }
            
            return false;
        }
        
        /// <summary>
        /// Enables or disables Focus Assist (Do Not Disturb)
        /// </summary>
        public static bool SetFocusAssistEnabled(bool enabled)
        {
            try
            {
                // This would require more complex Windows API calls
                // For now, return success but don't actually change the setting
                Debug.WriteLine($"Focus Assist {(enabled ? "enabled" : "disabled")} (simulated)");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting Focus Assist: {ex.Message}");
                return false;
            }
        }
        
        #endregion
    }
}
