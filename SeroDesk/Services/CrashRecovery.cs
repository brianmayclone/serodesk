using System;
using System.IO;

namespace SeroDesk.Services
{
    /// <summary>
    /// Provides crash recovery by maintaining a flag file that indicates SeroDesk is running.
    /// If the flag file exists on startup, it means the previous session crashed without cleanup.
    /// </summary>
    public static class CrashRecovery
    {
        private static readonly string FlagPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SeroDesk", "running.flag");

        private static readonly string StatePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SeroDesk", "shell_state.json");

        /// <summary>
        /// Checks if a previous session crashed (flag file still exists).
        /// If so, restores the taskbar and Explorer before continuing.
        /// </summary>
        public static bool CheckAndRecoverFromCrash()
        {
            try
            {
                if (File.Exists(FlagPath))
                {
                    System.Diagnostics.Debug.WriteLine("CrashRecovery: Previous session did not exit cleanly. Recovering...");

                    // Restore taskbar
                    try { Platform.TaskbarManager.ShowTaskbar(); } catch { }

                    // Restart Explorer if it was killed
                    try
                    {
                        var explorerProcesses = System.Diagnostics.Process.GetProcessesByName("explorer");
                        if (explorerProcesses.Length == 0)
                        {
                            System.Diagnostics.Process.Start("explorer.exe");
                            System.Threading.Thread.Sleep(2000);
                        }
                    }
                    catch { }

                    // Clean up stale flag
                    try { File.Delete(FlagPath); } catch { }

                    return true; // crash was recovered
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CrashRecovery: Error checking crash state: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// Creates the running flag file. Call this after all services are initialized.
        /// </summary>
        public static void SetRunning()
        {
            try
            {
                var dir = Path.GetDirectoryName(FlagPath)!;
                Directory.CreateDirectory(dir);
                File.WriteAllText(FlagPath, $"PID={Environment.ProcessId}\nStarted={DateTime.Now:O}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CrashRecovery: Error setting running flag: {ex.Message}");
            }
        }

        /// <summary>
        /// Removes the running flag file. Call this during clean shutdown.
        /// </summary>
        public static void SetStopped()
        {
            try
            {
                if (File.Exists(FlagPath))
                    File.Delete(FlagPath);
            }
            catch { }
        }
    }
}
