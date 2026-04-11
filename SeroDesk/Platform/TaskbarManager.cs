using System;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

namespace SeroDesk.Platform
{
    /// <summary>
    /// Manages Windows taskbar visibility for shell replacement.
    /// Uses registry-based auto-hide + window hiding for maximum reliability on Windows 11.
    /// </summary>
    public static class TaskbarManager
    {
        private static bool _isTaskbarHidden = false;
        private static byte[]? _originalStuckRectsValue;
        private static readonly string StuckRectsKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StuckRects3";

        public static bool IsTaskbarHidden => _isTaskbarHidden;

        /// <summary>
        /// Hides the Windows taskbar using registry auto-hide + window manipulation.
        /// This is the most reliable method for Windows 10/11.
        /// </summary>
        public static bool HideTaskbar()
        {
            try
            {
                // Step 1: Set taskbar to auto-hide via registry (most reliable on Win11)
                SetTaskbarAutoHideViaRegistry(true);

                // Step 2: Hide the taskbar windows directly
                HideTaskbarWindows();

                // Step 3: Notify Explorer of the change
                NotifyExplorerOfChange();

                _isTaskbarHidden = true;
                Services.Logger.Info("Taskbar hidden via registry auto-hide + window hiding");
                return true;
            }
            catch (Exception ex)
            {
                Services.Logger.Error("Failed to hide taskbar", ex);

                // Fallback: Just try window hiding
                try
                {
                    HideTaskbarWindows();
                    _isTaskbarHidden = true;
                    return true;
                }
                catch { return false; }
            }
        }

        /// <summary>
        /// Restores the taskbar to its original visible state.
        /// </summary>
        public static bool ShowTaskbar()
        {
            try
            {
                // Restore auto-hide setting
                SetTaskbarAutoHideViaRegistry(false);

                // Show taskbar windows
                ShowTaskbarWindows();

                // Notify Explorer
                NotifyExplorerOfChange();

                _isTaskbarHidden = false;
                Services.Logger.Info("Taskbar restored");
                return true;
            }
            catch (Exception ex)
            {
                Services.Logger.Error("Failed to restore taskbar", ex);
                return false;
            }
        }

        /// <summary>
        /// Force re-hides the taskbar. Use when it reappears unexpectedly.
        /// </summary>
        public static void ForceHideTaskbar()
        {
            try
            {
                // Don't reset _isTaskbarHidden - just re-apply hiding
                HideTaskbarWindows();

                // Also re-check registry setting
                if (!IsAutoHideEnabled())
                {
                    SetTaskbarAutoHideViaRegistry(true);
                    NotifyExplorerOfChange();
                }
            }
            catch (Exception ex)
            {
                Services.Logger.Error("Force hide taskbar failed", ex);
            }
        }

        /// <summary>
        /// Sets the taskbar auto-hide behavior via the Windows registry.
        /// This is the most reliable method because it makes Windows itself manage the hiding.
        /// </summary>
        private static void SetTaskbarAutoHideViaRegistry(bool autoHide)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(StuckRectsKey, writable: true);
                if (key == null) return;

                var settings = key.GetValue("Settings") as byte[];
                if (settings == null || settings.Length < 9) return;

                // Save original value for restoration
                if (_originalStuckRectsValue == null)
                    _originalStuckRectsValue = (byte[])settings.Clone();

                // Byte 8 controls the auto-hide behavior:
                // Bit 0 (0x01) = locked
                // Bit 1 (0x02) = auto-hide enabled
                // Bit 3 (0x08) = always on top
                if (autoHide)
                    settings[8] |= 0x03; // Set auto-hide + locked
                else
                    settings[8] = _originalStuckRectsValue[8]; // Restore original

                key.SetValue("Settings", settings);
            }
            catch (Exception ex)
            {
                Services.Logger.Error("Registry auto-hide failed", ex);
            }
        }

        private static bool IsAutoHideEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(StuckRectsKey);
                var settings = key?.GetValue("Settings") as byte[];
                if (settings != null && settings.Length > 8)
                    return (settings[8] & 0x02) != 0;
            }
            catch { }
            return false;
        }

        private static void HideTaskbarWindows()
        {
            // Hide main taskbar
            var taskbar = FindWindowW("Shell_TrayWnd", null);
            if (taskbar != IntPtr.Zero)
            {
                ShowWindow(taskbar, SW_HIDE);
            }

            // Hide Start button
            var startBtn = FindWindowW("Button", "Start");
            if (startBtn != IntPtr.Zero)
                ShowWindow(startBtn, SW_HIDE);

            // Hide secondary taskbars (multi-monitor)
            EnumWindows((hWnd, lParam) =>
            {
                var className = GetClassNameStr(hWnd);
                if (className == "Shell_SecondaryTrayWnd")
                    ShowWindow(hWnd, SW_HIDE);
                return true;
            }, IntPtr.Zero);
        }

        private static void ShowTaskbarWindows()
        {
            var taskbar = FindWindowW("Shell_TrayWnd", null);
            if (taskbar != IntPtr.Zero)
                ShowWindow(taskbar, SW_SHOW);

            var startBtn = FindWindowW("Button", "Start");
            if (startBtn != IntPtr.Zero)
                ShowWindow(startBtn, SW_SHOW);

            EnumWindows((hWnd, lParam) =>
            {
                var className = GetClassNameStr(hWnd);
                if (className == "Shell_SecondaryTrayWnd")
                    ShowWindow(hWnd, SW_SHOW);
                return true;
            }, IntPtr.Zero);
        }

        /// <summary>
        /// Notifies Explorer that taskbar settings have changed by broadcasting a message.
        /// </summary>
        private static void NotifyExplorerOfChange()
        {
            try
            {
                // Broadcast WM_SETTINGCHANGE to all windows
                SendMessageTimeout(
                    HWND_BROADCAST,
                    WM_SETTINGCHANGE,
                    IntPtr.Zero,
                    "TraySettings",
                    SMTO_ABORTIFHUNG,
                    1000,
                    out _);
            }
            catch { }
        }

        private static string GetClassNameStr(IntPtr hWnd)
        {
            var sb = new StringBuilder(256);
            GetClassNameW(hWnd, sb, sb.Capacity);
            return sb.ToString();
        }

        #region P/Invoke

        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;
        private static readonly IntPtr HWND_BROADCAST = new IntPtr(0xFFFF);
        private const uint WM_SETTINGCHANGE = 0x001A;
        private const uint SMTO_ABORTIFHUNG = 0x0002;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr FindWindowW(string? lpClassName, string? lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(NativeMethods.EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetClassNameW(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam,
            string lParam, uint fuFlags, uint uTimeout, out IntPtr lpdwResult);

        #endregion
    }
}
