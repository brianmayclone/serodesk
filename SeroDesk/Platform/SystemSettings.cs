using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace SeroDesk.Platform
{
    public static class SystemSettings
    {
        [DllImport("powrprof.dll", SetLastError = true)]
        public static extern uint PowerSetActiveScheme(IntPtr UserRootPowerKey, ref Guid SchemeGuid);
        
        [DllImport("user32.dll")]
        public static extern int SystemParametersInfo(int uAction, int uParam, ref int lpvParam, int fuWinIni);
        
        [DllImport("winmm.dll")]
        public static extern int waveOutSetVolume(IntPtr hwo, uint dwVolume);
        
        [DllImport("winmm.dll")]
        public static extern int waveOutGetVolume(IntPtr hwo, out uint dwVolume);
        
        private const int SPI_GETSCREENBRIGHTNESS = 0x0073;
        private const int SPI_SETSCREENBRIGHTNESS = 0x0074;
        private const int SPIF_UPDATEINIFILE = 0x01;
        private const int SPIF_SENDCHANGE = 0x02;
        
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