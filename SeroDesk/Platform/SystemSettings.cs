using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace SeroDesk.Platform
{
    /// <summary>
    /// Provides access to Windows system settings and hardware controls for SeroDesk interface integration.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The SystemSettings class offers comprehensive control over Windows system settings that are
    /// commonly accessed through iOS/Android-style control panels and quick settings interfaces:
    /// <list type="bullet">
    /// <item>Network connectivity (WiFi, Bluetooth, Ethernet, Airplane Mode)</item>
    /// <item>Display settings (brightness control and orientation lock)</item>
    /// <item>Audio settings (system volume control)</item>
    /// <item>Mobile hotspot functionality for wireless sharing</item>
    /// <item>System integration for shell replacement scenarios</item>
    /// </list>
    /// </para>
    /// <para>
    /// The class uses a combination of Windows APIs, registry access, PowerShell commands, and
    /// system utilities (netsh) to provide reliable access to system settings. Many operations
    /// require elevated privileges and will prompt for administrator access when needed.
    /// </para>
    /// <para>
    /// <strong>IMPORTANT:</strong> This class performs system-level operations that can affect
    /// network connectivity, display settings, and audio. All methods include error handling
    /// to prevent system instability, but administrative privileges may be required.
    /// </para>
    /// </remarks>
    public static class SystemSettings
    {
        /// <summary>
        /// Sets the active Windows power scheme.
        /// </summary>
        [DllImport("powrprof.dll", SetLastError = true)]
        public static extern uint PowerSetActiveScheme(IntPtr UserRootPowerKey, ref Guid SchemeGuid);
        
        /// <summary>
        /// Retrieves or sets system-wide parameters including display brightness.
        /// </summary>
        [DllImport("user32.dll")]
        public static extern int SystemParametersInfo(int uAction, int uParam, ref int lpvParam, int fuWinIni);
        
        /// <summary>
        /// Sets the volume level for the default audio output device.
        /// </summary>
        [DllImport("winmm.dll")]
        public static extern int waveOutSetVolume(IntPtr hwo, uint dwVolume);
        
        /// <summary>
        /// Gets the volume level for the default audio output device.
        /// </summary>
        [DllImport("winmm.dll")]
        public static extern int waveOutGetVolume(IntPtr hwo, out uint dwVolume);
        
        private const int SPI_GETSCREENBRIGHTNESS = 0x0073;
        private const int SPI_SETSCREENBRIGHTNESS = 0x0074;
        private const int SPIF_UPDATEINIFILE = 0x01;
        private const int SPIF_SENDCHANGE = 0x02;
        
        /// <summary>
        /// Determines whether WiFi connectivity is currently enabled and connected.
        /// </summary>
        /// <returns>True if WiFi is enabled and connected; otherwise, false.</returns>
        /// <remarks>
        /// This method uses the netsh command-line utility to query network interface status.
        /// If the query fails, it defaults to returning true to prevent connectivity issues.
        /// </remarks>
        public static bool IsWiFiEnabled()
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "netsh",
                        Arguments = "interface show interface",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };
                
                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                
                return output.Contains("Wi-Fi") && output.Contains("Connected");
            }
            catch
            {
                return true; // Default to enabled if check fails
            }
        }
        
        /// <summary>
        /// Enables or disables the WiFi network interface.
        /// </summary>
        /// <param name="enabled">True to enable WiFi; false to disable it.</param>
        /// <remarks>
        /// <para>
        /// This method requires administrative privileges and will prompt for elevation.
        /// The operation uses netsh to control the WiFi interface state.
        /// </para>
        /// <para>
        /// <strong>WARNING:</strong> Disabling WiFi will disconnect all wireless network connections
        /// and may leave the system without network connectivity if no wired connection is available.
        /// </para>
        /// </remarks>
        public static void SetWiFiEnabled(bool enabled)
        {
            try
            {
                var action = enabled ? "enable" : "disable";
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "netsh",
                        Arguments = $"interface set interface \"Wi-Fi\" {action}",
                        UseShellExecute = true,
                        CreateNoWindow = true,
                        Verb = "runas" // Run as administrator
                    }
                };
                
                process.Start();
                process.WaitForExit();
            }
            catch
            {
                // Silently fail if unable to change WiFi state
            }
        }
        
        /// <summary>
        /// Determines whether Bluetooth functionality is available and enabled on the system.
        /// </summary>
        /// <returns>True if Bluetooth is available and enabled; otherwise, false.</returns>
        /// <remarks>
        /// <para>
        /// This method checks the Windows registry for Bluetooth driver support.
        /// The current implementation provides a simplified check that assumes Bluetooth
        /// is enabled if the hardware and drivers are present.
        /// </para>
        /// <para>
        /// For more accurate state detection, this method could be enhanced to use
        /// Windows Runtime APIs or WMI queries to check the actual radio state.
        /// </para>
        /// </remarks>
        public static bool IsBluetoothEnabled()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\BTHPORT\Parameters\Radio Support");
                if (key?.GetValue("SupportDLL") != null)
                {
                    // Simplified check - assume Bluetooth is available and enabled by default
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Enables or disables Bluetooth functionality on the system.
        /// </summary>
        /// <param name="enabled">True to enable Bluetooth; false to disable it.</param>
        /// <remarks>
        /// <para>
        /// This method uses PowerShell with Windows Runtime APIs to control Bluetooth radio state.
        /// The operation may require administrative privileges and user consent for radio access.
        /// </para>
        /// <para>
        /// <strong>NOTE:</strong> Bluetooth control through Windows Runtime APIs may not work
        /// on all systems or Windows versions. The method fails silently if the operation
        /// cannot be completed.
        /// </para>
        /// </remarks>
        public static void SetBluetoothEnabled(bool enabled)
        {
            try
            {
                // This is a simplified implementation
                // Real Bluetooth control would require more complex Windows API calls
                var action = enabled ? "on" : "off";
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "powershell",
                        Arguments = $"-Command \"Add-Type -AssemblyName System.Runtime.WindowsRuntime; [Windows.Devices.Radios.Radio,Windows.System.Devices,ContentType=WindowsRuntime] | Out-Null; [Windows.Devices.Radios.RadioManager,Windows.System.Devices,ContentType=WindowsRuntime] | Out-Null; $radios = [Windows.Devices.Radios.RadioManager]::GetRadiosAsync(); $radios.GetResults() | Where-Object Kind -eq 'Bluetooth' | ForEach-Object {{ $_.SetStateAsync('{(enabled ? "On" : "Off")}') }}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                
                process.Start();
                process.WaitForExit();
            }
            catch
            {
                // Silently fail if unable to change Bluetooth state
            }
        }
        
        /// <summary>
        /// Determines whether the primary wired network connection (Ethernet) is enabled and connected.
        /// </summary>
        /// <returns>True if Ethernet is enabled and connected; otherwise, false.</returns>
        /// <remarks>
        /// <para>
        /// On desktop Windows systems, this method represents the wired Ethernet connection
        /// rather than mobile/cellular data. The method queries network interface status
        /// using netsh to determine connectivity state.
        /// </para>
        /// <para>
        /// This mapping aligns with mobile device paradigms where "Mobile Data" represents
        /// the primary internet connection alternative to WiFi.
        /// </para>
        /// </remarks>
        public static bool IsMobileDataEnabled()
        {
            // For desktop Windows, this would typically represent ethernet/wired connection
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "netsh",
                        Arguments = "interface show interface",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };
                
                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                
                return output.Contains("Ethernet") && output.Contains("Connected");
            }
            catch
            {
                return true;
            }
        }
        
        /// <summary>
        /// Enables or disables the primary wired network connection (Ethernet).
        /// </summary>
        /// <param name="enabled">True to enable Ethernet; false to disable it.</param>
        /// <remarks>
        /// <para>
        /// This method controls the Ethernet network interface, requiring administrative privileges.
        /// On desktop systems, this represents the primary wired internet connection.
        /// </para>
        /// <para>
        /// <strong>CAUTION:</strong> Disabling Ethernet may result in complete loss of network
        /// connectivity if WiFi is not available or configured.
        /// </para>
        /// </remarks>
        public static void SetMobileDataEnabled(bool enabled)
        {
            try
            {
                var action = enabled ? "enable" : "disable";
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "netsh",
                        Arguments = $"interface set interface \"Ethernet\" {action}",
                        UseShellExecute = true,
                        CreateNoWindow = true,
                        Verb = "runas"
                    }
                };
                
                process.Start();
                process.WaitForExit();
            }
            catch
            {
                // Silently fail
            }
        }
        
        /// <summary>
        /// Determines whether Airplane Mode is currently enabled on the system.
        /// </summary>
        /// <returns>True if Airplane Mode is enabled; otherwise, false.</returns>
        /// <remarks>
        /// <para>
        /// This method checks the Windows registry for radio management settings to determine
        /// if Airplane Mode is active. When enabled, Airplane Mode typically disables all
        /// wireless communications including WiFi, Bluetooth, and cellular radios.
        /// </para>
        /// <para>
        /// The registry key checked may vary between Windows versions, and this implementation
        /// provides basic detection that may need updates for different system configurations.
        /// </para>
        /// </remarks>
        public static bool IsAirplaneModeEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"System\CurrentControlSet\Control\RadioManagement\SystemRadioState");
                if (key != null)
                {
                    var value = key.GetValue("") as int?;
                    return value == 0; // 0 = airplane mode on, 1 = airplane mode off
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Enables or disables Airplane Mode on the system.
        /// </summary>
        /// <param name="enabled">True to enable Airplane Mode; false to disable it.</param>
        /// <remarks>
        /// <para>
        /// This method uses PowerShell to control network adapter state as a simplified
        /// implementation of Airplane Mode functionality. When enabled, it disables all
        /// network adapters; when disabled, it enables them.
        /// </para>
        /// <para>
        /// <strong>IMPORTANT:</strong> This operation requires administrative privileges and
        /// will affect all network connectivity. True Airplane Mode implementation would
        /// require deeper integration with Windows radio management APIs.
        /// </para>
        /// </remarks>
        public static void SetAirplaneModeEnabled(bool enabled)
        {
            try
            {
                // This is a simplified implementation
                // Real airplane mode would require system-level changes
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "powershell",
                        Arguments = enabled ? 
                            "-Command \"Get-NetAdapter | Disable-NetAdapter -Confirm:$false\"" :
                            "-Command \"Get-NetAdapter | Enable-NetAdapter -Confirm:$false\"",
                        UseShellExecute = true,
                        CreateNoWindow = true,
                        Verb = "runas"
                    }
                };
                
                process.Start();
                process.WaitForExit();
            }
            catch
            {
                // Silently fail
            }
        }
        
        /// <summary>
        /// Determines whether the mobile hotspot (hosted network) is currently active.
        /// </summary>
        /// <returns>True if the mobile hotspot is enabled and started; otherwise, false.</returns>
        /// <remarks>
        /// <para>
        /// This method uses the netsh wlan command to query the hosted network status.
        /// The hosted network feature allows Windows to create a WiFi access point that
        /// other devices can connect to for internet sharing.
        /// </para>
        /// <para>
        /// The hotspot functionality requires compatible wireless hardware and drivers
        /// that support hosted network mode (also known as SoftAP).
        /// </para>
        /// </remarks>
        public static bool IsHotspotEnabled()
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "netsh",
                        Arguments = "wlan show hostednetwork",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };
                
                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                
                return output.Contains("Status") && output.Contains("Started");
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Enables or disables the mobile hotspot (hosted network) functionality.
        /// </summary>
        /// <param name="enabled">True to start the mobile hotspot; false to stop it.</param>
        /// <remarks>
        /// <para>
        /// This method requires administrative privileges and uses netsh to control the
        /// Windows hosted network feature. The hotspot must be configured before it
        /// can be started (SSID, password, etc.).
        /// </para>
        /// <para>
        /// <strong>NOTE:</strong> Starting a hotspot may affect existing WiFi connections
        /// and requires compatible wireless hardware. Some systems may not support
        /// hosted network functionality due to driver limitations.
        /// </para>
        /// </remarks>
        public static void SetHotspotEnabled(bool enabled)
        {
            try
            {
                var action = enabled ? "start" : "stop";
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "netsh",
                        Arguments = $"wlan {action} hostednetwork",
                        UseShellExecute = true,
                        CreateNoWindow = true,
                        Verb = "runas"
                    }
                };
                
                process.Start();
                process.WaitForExit();
            }
            catch
            {
                // Silently fail
            }
        }
        
        /// <summary>
        /// Determines whether screen orientation is locked (tablet mode active).
        /// </summary>
        /// <returns>True if orientation is locked/tablet mode is active; otherwise, false.</returns>
        /// <remarks>
        /// <para>
        /// This method checks the Windows registry for tablet mode settings, which
        /// controls screen orientation behavior on convertible devices and tablets.
        /// When enabled, the screen orientation is typically locked to prevent
        /// automatic rotation.
        /// </para>
        /// <para>
        /// On desktop systems, this setting may have limited effect but is included
        /// for compatibility with mobile-style interfaces.
        /// </para>
        /// </remarks>
        public static bool IsOrientationLocked()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\ImmersiveShell");
                if (key != null)
                {
                    var value = key.GetValue("TabletMode") as int?;
                    return value == 1;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Enables or disables screen orientation lock (tablet mode).
        /// </summary>
        /// <param name="locked">True to lock orientation/enable tablet mode; false to allow free rotation.</param>
        /// <remarks>
        /// <para>
        /// This method modifies the Windows registry to control tablet mode settings.
        /// The change affects how Windows handles screen rotation on convertible
        /// devices and may influence UI behavior in applications.
        /// </para>
        /// <para>
        /// Registry changes may require an application restart or system reboot
        /// to take full effect, depending on the system configuration.
        /// </para>
        /// </remarks>
        public static void SetOrientationLocked(bool locked)
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\ImmersiveShell");
                key?.SetValue("TabletMode", locked ? 1 : 0, RegistryValueKind.DWord);
            }
            catch
            {
                // Silently fail
            }
        }
        
        /// <summary>
        /// Retrieves the current display brightness level.
        /// </summary>
        /// <returns>The current brightness level as a percentage (0-100), or 75 if detection fails.</returns>
        /// <remarks>
        /// <para>
        /// This method uses the SystemParametersInfo Windows API to query the current
        /// display brightness setting. The returned value represents the brightness
        /// as a percentage where 0 is minimum brightness and 100 is maximum.
        /// </para>
        /// <para>
        /// If brightness detection fails (e.g., on systems without adjustable brightness),
        /// the method returns a default value of 75% to ensure reasonable visibility.
        /// </para>
        /// </remarks>
        public static int GetBrightnessLevel()
        {
            try
            {
                int brightness = 0;
                SystemParametersInfo(SPI_GETSCREENBRIGHTNESS, 0, ref brightness, 0);
                return brightness;
            }
            catch
            {
                return 75; // Default brightness
            }
        }
        
        /// <summary>
        /// Sets the display brightness level.
        /// </summary>
        /// <param name="level">The desired brightness level as a percentage (0-100).</param>
        /// <remarks>
        /// <para>
        /// This method uses the SystemParametersInfo Windows API to adjust display brightness.
        /// The level parameter is automatically clamped to the valid range of 0-100 to
        /// prevent invalid values.
        /// </para>
        /// <para>
        /// <strong>NOTE:</strong> Brightness control may not be available on all systems,
        /// particularly desktop computers with external monitors. The method fails silently
        /// if brightness adjustment is not supported by the hardware.
        /// </para>
        /// </remarks>
        public static void SetBrightnessLevel(int level)
        {
            try
            {
                level = Math.Max(0, Math.Min(100, level));
                SystemParametersInfo(SPI_SETSCREENBRIGHTNESS, level, ref level, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
            }
            catch
            {
                // Silently fail
            }
        }
        
        /// <summary>
        /// Retrieves the current system volume level.
        /// </summary>
        /// <returns>The current volume level as a percentage (0-100), or 50 if detection fails.</returns>
        /// <remarks>
        /// <para>
        /// This method uses the Windows Multimedia API (winmm.dll) to query the current
        /// system volume level. The returned value represents the left channel volume
        /// converted to a percentage where 0 is muted and 100 is maximum volume.
        /// </para>
        /// <para>
        /// If volume detection fails (e.g., due to audio system issues), the method
        /// returns a default value of 50% to provide a reasonable starting point.
        /// </para>
        /// </remarks>
        public static int GetVolumeLevel()
        {
            try
            {
                waveOutGetVolume(IntPtr.Zero, out uint volume);
                
                // Volume is a 32-bit value where the low-order word contains the left channel
                // and the high-order word contains the right channel volume
                var leftChannel = volume & 0x0000FFFF;
                
                // Convert from 0-65535 range to 0-100 range
                return (int)((leftChannel / 65535.0) * 100);
            }
            catch
            {
                return 50; // Default volume
            }
        }
        
        /// <summary>
        /// Sets the system volume level.
        /// </summary>
        /// <param name="level">The desired volume level as a percentage (0-100).</param>
        /// <remarks>
        /// <para>
        /// This method uses the Windows Multimedia API to adjust system volume.
        /// The level parameter is automatically clamped to the valid range of 0-100
        /// and applied to both left and right audio channels equally.
        /// </para>
        /// <para>
        /// The volume change takes effect immediately and affects all system audio output.
        /// Individual application volumes are not modified by this operation.
        /// </para>
        /// <para>
        /// <strong>NOTE:</strong> This method controls the master system volume and may not
        /// work correctly with some advanced audio drivers or virtual audio devices.
        /// </para>
        /// </remarks>
        public static void SetVolumeLevel(int level)
        {
            try
            {
                level = Math.Max(0, Math.Min(100, level));
                
                // Convert from 0-100 range to 0-65535 range
                var volumeValue = (uint)((level / 100.0) * 65535);
                
                // Set both left and right channels to the same volume
                var stereoVolume = (volumeValue << 16) | volumeValue;
                
                waveOutSetVolume(IntPtr.Zero, stereoVolume);
            }
            catch
            {
                // Silently fail
            }
        }
    }
}