using System.Diagnostics;

namespace SeroDesk.Services
{
    public class ExplorerManager
    {
        private static ExplorerManager? _instance;
        public static ExplorerManager Instance => _instance ??= new ExplorerManager();

        private bool _explorerWasRunning = false;

        public void KillExplorer()
        {
            try
            {
                var explorerProcesses = Process.GetProcessesByName("explorer");
                _explorerWasRunning = explorerProcesses.Length > 0;

                foreach (var process in explorerProcesses)
                {
                    try
                    {
                        process.Kill();
                        process.WaitForExit(5000); // Wait max 5 seconds
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error killing explorer process: {ex.Message}");
                    }
                }

                // Also kill any remaining Windows Shell processes
                KillWindowsShellProcesses();

                if (_explorerWasRunning)
                {
                    System.Diagnostics.Debug.WriteLine("Explorer processes terminated successfully");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in KillExplorer: {ex.Message}");
            }
        }
        
        private void KillWindowsShellProcesses()
        {
            try
            {
                // Kill additional Windows shell processes that might show taskbar
                string[] shellProcesses = { "dwm", "winlogon", "csrss" };
                
                foreach (var processName in shellProcesses)
                {
                    try
                    {
                        var processes = Process.GetProcessesByName(processName);
                        // Don't kill critical system processes, just log them
                        System.Diagnostics.Debug.WriteLine($"Found {processes.Length} {processName} processes (not terminating system processes)");
                    }
                    catch { }
                }
                
                // Hide the taskbar instead of killing critical processes
                HideWindowsTaskbar();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in KillWindowsShellProcesses: {ex.Message}");
            }
        }
        
        private void HideWindowsTaskbar()
        {
            try
            {
                // Find and hide the Windows taskbar
                IntPtr taskbarHandle = FindWindow("Shell_TrayWnd", null);
                if (taskbarHandle != IntPtr.Zero)
                {
                    ShowWindow(taskbarHandle, SW_HIDE);
                    System.Diagnostics.Debug.WriteLine("Windows taskbar hidden");
                }
                
                // Hide start button
                IntPtr startButtonHandle = FindWindow("Button", "Start");
                if (startButtonHandle != IntPtr.Zero)
                {
                    ShowWindow(startButtonHandle, SW_HIDE);
                }
                
                // Hide Windows Start Menu if visible
                HideStartMenu();
                
                // Block Windows key to prevent start menu
                BlockWindowsKey();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error hiding taskbar: {ex.Message}");
            }
        }
        
        private void HideStartMenu()
        {
            try
            {
                // Find Windows Start Menu windows
                IntPtr startMenuHandle = FindWindow("Windows.UI.Core.CoreWindow", "Start");
                if (startMenuHandle != IntPtr.Zero)
                {
                    ShowWindow(startMenuHandle, SW_HIDE);
                    System.Diagnostics.Debug.WriteLine("Windows Start Menu hidden");
                }
                
                // Also check for classic start menu
                IntPtr classicStartHandle = FindWindow("DV2ControlHost", null);
                if (classicStartHandle != IntPtr.Zero)
                {
                    ShowWindow(classicStartHandle, SW_HIDE);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error hiding start menu: {ex.Message}");
            }
        }
        
        private void BlockWindowsKey()
        {
            try
            {
                // Install low-level keyboard hook to block Windows key
                InstallKeyboardHook();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error installing keyboard hook: {ex.Message}");
            }
        }
        
        private void InstallKeyboardHook()
        {
            // This would require implementing a low-level keyboard hook
            // For now, we'll use a simpler approach of registering hotkeys
            RegisterHotKey(IntPtr.Zero, 1, 0, VK_LWIN);
            RegisterHotKey(IntPtr.Zero, 2, 0, VK_RWIN);
        }

        public void RestartExplorer()
        {
            try
            {
                // Show taskbar before restarting explorer
                ShowWindowsTaskbar();

                // Wait a moment for cleanup
                System.Threading.Thread.Sleep(1000);

                // Start explorer again
                var startInfo = new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    UseShellExecute = true,
                    CreateNoWindow = false
                };

                Process.Start(startInfo);
                System.Diagnostics.Debug.WriteLine("Explorer restarted successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error restarting explorer: {ex.Message}");
            }
        }
        
        private void ShowWindowsTaskbar()
        {
            try
            {
                // Unregister hotkeys first
                UnregisterHotKey(IntPtr.Zero, 1);
                UnregisterHotKey(IntPtr.Zero, 2);
                
                // Show the Windows taskbar again
                IntPtr taskbarHandle = FindWindow("Shell_TrayWnd", null);
                if (taskbarHandle != IntPtr.Zero)
                {
                    ShowWindow(taskbarHandle, SW_SHOW);
                    System.Diagnostics.Debug.WriteLine("Windows taskbar shown");
                }
                
                // Also show start button
                IntPtr startButtonHandle = FindWindow("Button", "Start");
                if (startButtonHandle != IntPtr.Zero)
                {
                    ShowWindow(startButtonHandle, SW_SHOW);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing taskbar: {ex.Message}");
            }
        }

        public bool IsExplorerRunning()
        {
            try
            {
                var explorerProcesses = Process.GetProcessesByName("explorer");
                return explorerProcesses.Length > 0;
            }
            catch
            {
                return false;
            }
        }

        public bool WasExplorerInitiallyRunning => _explorerWasRunning;
        
        #region P/Invoke for taskbar hiding
        
        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);
        
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        
        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;
        
        // Virtual key codes for Windows keys
        private const int VK_LWIN = 0x5B;
        private const int VK_RWIN = 0x5C;
        
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, int vk);
        
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        
        #endregion
    }
}