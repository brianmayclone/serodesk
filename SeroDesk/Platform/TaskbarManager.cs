using System.Runtime.InteropServices;

namespace SeroDesk.Platform
{
    /// <summary>
    /// Manages Windows taskbar visibility and behavior for shell replacement functionality.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The TaskbarManager provides comprehensive control over the Windows taskbar to enable
    /// SeroDesk's shell replacement capabilities. It can completely hide or show the taskbar
    /// and associated shell elements:
    /// <list type="bullet">
    /// <item>Complete taskbar hiding using multiple redundant methods</item>
    /// <item>Start button visibility control</item>
    /// <item>Multi-monitor secondary taskbar management</item>
    /// <item>Reliable taskbar restoration when SeroDesk exits</item>
    /// <item>Forced refresh capabilities for stubborn system states</item>
    /// </list>
    /// </para>
    /// <para>
    /// The class uses multiple hiding techniques simultaneously to ensure the taskbar
    /// remains hidden even when Windows attempts to restore it. This includes setting
    /// window visibility, moving windows off-screen, and modifying window styles.
    /// </para>
    /// <para>
    /// <strong>IMPORTANT:</strong> Improper use of this class can leave the user without
    /// access to the Windows taskbar. Always ensure proper restoration mechanisms are
    /// in place before hiding the taskbar.
    /// </para>
    /// </remarks>
    public static class TaskbarManager
    {
        private static IntPtr _taskbarHandle = IntPtr.Zero;
        private static IntPtr _startButtonHandle = IntPtr.Zero;
        private static bool _isTaskbarHidden = false;
        
        /// <summary>
        /// Gets the handle to the Windows taskbar window, caching it for performance.
        /// </summary>
        /// <value>The window handle to the main Windows taskbar (Shell_TrayWnd).</value>
        /// <remarks>
        /// This property uses lazy initialization to find and cache the taskbar window handle.
        /// The handle is obtained by searching for the "Shell_TrayWnd" window class.
        /// </remarks>
        private static IntPtr TaskbarHandle
        {
            get
            {
                if (_taskbarHandle == IntPtr.Zero)
                {
                    _taskbarHandle = NativeMethods.FindWindow("Shell_TrayWnd", string.Empty);
                }
                return _taskbarHandle;
            }
        }
        
        /// <summary>
        /// Gets the handle to the Windows Start button, caching it for performance.
        /// </summary>
        /// <value>The window handle to the Start button control.</value>
        /// <remarks>
        /// This property uses lazy initialization to find and cache the Start button handle.
        /// The handle is obtained by searching for a "Button" window with "Start" text.
        /// </remarks>
        private static IntPtr StartButtonHandle
        {
            get
            {
                if (_startButtonHandle == IntPtr.Zero)
                {
                    _startButtonHandle = NativeMethods.FindWindow("Button", "Start");
                }
                return _startButtonHandle;
            }
        }
        
        /// <summary>
        /// Completely hides the Windows taskbar using multiple redundant techniques.
        /// </summary>
        /// <returns>True if the taskbar was successfully hidden; otherwise, false.</returns>
        /// <remarks>
        /// <para>
        /// This method employs three different hiding techniques simultaneously to ensure
        /// the taskbar remains hidden even when Windows attempts to restore it:
        /// <list type="number">
        /// <item>Sets the taskbar window to hidden state (SW_HIDE)</item>
        /// <item>Moves the taskbar window far off-screen (-32000, -32000)</item>
        /// <item>Removes the WS_VISIBLE style flag from the window</item>
        /// </list>
        /// </para>
        /// <para>
        /// The method also hides the Start button and any secondary taskbars on
        /// multi-monitor setups to provide complete shell replacement.
        /// </para>
        /// <para>
        /// <strong>WARNING:</strong> This operation removes user access to the Windows
        /// taskbar and Start menu. Ensure proper restoration mechanisms are available.
        /// </para>
        /// </remarks>
        public static bool HideTaskbar()
        {
            if (_isTaskbarHidden) return true;
            
            try
            {
                var taskbarHandle = TaskbarHandle;
                if (taskbarHandle == IntPtr.Zero) return false;
                
                // Method 1: Hide the taskbar window completely
                NativeMethods.ShowWindow(taskbarHandle, NativeMethods.SW_HIDE);
                
                // Method 2: Move taskbar off-screen
                NativeMethods.SetWindowPos(taskbarHandle, IntPtr.Zero, -32000, -32000, 0, 0, 
                    NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOZORDER | NativeMethods.SWP_NOACTIVATE);
                
                // Method 3: Set taskbar window style to be invisible
                var currentStyle = NativeMethods.GetWindowLong(taskbarHandle, NativeMethods.GWL_STYLE);
                NativeMethods.SetWindowLong(taskbarHandle, NativeMethods.GWL_STYLE, 
                    currentStyle & ~0x10000000); // Remove WS_VISIBLE
                
                // Also hide the Start button if it exists
                var startButtonHandle = StartButtonHandle;
                if (startButtonHandle != IntPtr.Zero)
                {
                    NativeMethods.ShowWindow(startButtonHandle, NativeMethods.SW_HIDE);
                }
                
                // Hide secondary taskbars on multi-monitor setups
                HideSecondaryTaskbars();
                
                _isTaskbarHidden = true;
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to hide taskbar: {ex.Message}");
            }
            
            return false;
        }
        
        /// <summary>
        /// Hides secondary taskbars on multi-monitor systems.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method enumerates all windows to find secondary taskbars (Shell_SecondaryTrayWnd)
        /// and applies the same hiding techniques used for the primary taskbar.
        /// </para>
        /// <para>
        /// Secondary taskbars appear on additional monitors in multi-monitor setups and
        /// must be hidden separately from the primary taskbar for complete shell replacement.
        /// </para>
        /// </remarks>
        private static void HideSecondaryTaskbars()
        {
            try
            {
                // Find and hide all secondary taskbars
                NativeMethods.EnumWindows((hWnd, lParam) =>
                {
                    var className = GetWindowClassName(hWnd);
                    if (className == "Shell_SecondaryTrayWnd")
                    {
                        NativeMethods.ShowWindow(hWnd, NativeMethods.SW_HIDE);
                        NativeMethods.SetWindowPos(hWnd, IntPtr.Zero, -32000, -32000, 0, 0,
                            NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOZORDER | NativeMethods.SWP_NOACTIVATE);
                    }
                    return true;
                }, IntPtr.Zero);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to hide secondary taskbars: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Retrieves the class name of a specified window.
        /// </summary>
        /// <param name="hWnd">The handle to the window whose class name is to be retrieved.</param>
        /// <returns>The class name of the specified window.</returns>
        /// <remarks>
        /// This helper method is used to identify window types during window enumeration,
        /// particularly for finding secondary taskbars by their "Shell_SecondaryTrayWnd" class name.
        /// </remarks>
        private static string GetWindowClassName(IntPtr hWnd)
        {
            var className = new System.Text.StringBuilder(256);
            NativeMethods.GetClassName(hWnd, className, className.Capacity);
            return className.ToString();
        }
        
        /// <summary>
        /// Restores the Windows taskbar to its normal visible state.
        /// </summary>
        /// <returns>True if the taskbar was successfully restored; otherwise, false.</returns>
        /// <remarks>
        /// <para>
        /// This method reverses all hiding operations performed by <see cref="HideTaskbar"/>:
        /// <list type="number">
        /// <item>Shows the taskbar window (SW_SHOW)</item>
        /// <item>Moves the taskbar back to its normal screen position</item>
        /// <item>Restores the WS_VISIBLE style flag</item>
        /// <item>Shows the Start button and secondary taskbars</item>
        /// </list>
        /// </para>
        /// <para>
        /// The taskbar is positioned at the bottom of the primary screen with standard
        /// dimensions. Multi-monitor secondary taskbars are also restored.
        /// </para>
        /// <para>
        /// This method should be called when SeroDesk exits or when the user needs
        /// to access the standard Windows shell interface.
        /// </para>
        /// </remarks>
        public static bool ShowTaskbar()
        {
            if (!_isTaskbarHidden) return true;
            
            try
            {
                var taskbarHandle = TaskbarHandle;
                if (taskbarHandle == IntPtr.Zero) return false;
                
                // Restore taskbar visibility
                NativeMethods.ShowWindow(taskbarHandle, NativeMethods.SW_SHOW);
                
                // Move taskbar back to bottom of screen
                var screenHeight = (int)System.Windows.SystemParameters.PrimaryScreenHeight;
                var screenWidth = (int)System.Windows.SystemParameters.PrimaryScreenWidth;
                NativeMethods.SetWindowPos(taskbarHandle, IntPtr.Zero, 0, screenHeight - 40, screenWidth, 40,
                    NativeMethods.SWP_NOZORDER | NativeMethods.SWP_NOACTIVATE);
                
                // Restore window style
                var currentStyle = NativeMethods.GetWindowLong(taskbarHandle, NativeMethods.GWL_STYLE);
                NativeMethods.SetWindowLong(taskbarHandle, NativeMethods.GWL_STYLE, 
                    currentStyle | 0x10000000); // Add WS_VISIBLE
                
                // Show the Start button
                var startButtonHandle = StartButtonHandle;
                if (startButtonHandle != IntPtr.Zero)
                {
                    NativeMethods.ShowWindow(startButtonHandle, NativeMethods.SW_SHOW);
                }
                
                // Show secondary taskbars
                ShowSecondaryTaskbars();
                
                _isTaskbarHidden = false;
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to show taskbar: {ex.Message}");
            }
            
            return false;
        }
        
        /// <summary>
        /// Restores visibility of secondary taskbars on multi-monitor systems.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method enumerates all windows to find hidden secondary taskbars
        /// (Shell_SecondaryTrayWnd) and restores their visibility using SW_SHOW.
        /// </para>
        /// <para>
        /// Secondary taskbars are automatically positioned by Windows on their
        /// respective monitors when made visible again.
        /// </para>
        /// </remarks>
        private static void ShowSecondaryTaskbars()
        {
            try
            {
                NativeMethods.EnumWindows((hWnd, lParam) =>
                {
                    var className = GetWindowClassName(hWnd);
                    if (className == "Shell_SecondaryTrayWnd")
                    {
                        NativeMethods.ShowWindow(hWnd, NativeMethods.SW_SHOW);
                    }
                    return true;
                }, IntPtr.Zero);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to show secondary taskbars: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Gets a value indicating whether the taskbar is currently hidden by this manager.
        /// </summary>
        /// <value>True if the taskbar has been hidden; otherwise, false.</value>
        /// <remarks>
        /// This property reflects the state managed by this class and may not account
        /// for taskbar visibility changes made by other applications or system events.
        /// </remarks>
        public static bool IsTaskbarHidden => _isTaskbarHidden;
        
        /// <summary>
        /// Forces a complete taskbar refresh and re-applies hiding operations.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method resets the internal state and re-discovers taskbar window handles
        /// before applying hiding operations. It is useful when:
        /// <list type="bullet">
        /// <item>The taskbar has reappeared due to system events</item>
        /// <item>Window handles have become invalid</item>
        /// <item>Explorer has been restarted</item>
        /// <item>The hiding state has become inconsistent</item>
        /// </list>
        /// </para>
        /// <para>
        /// This method provides a robust way to ensure the taskbar remains hidden
        /// even after system state changes that might restore taskbar visibility.
        /// </para>
        /// </remarks>
        public static void ForceHideTaskbar()
        {
            _isTaskbarHidden = false;
            _taskbarHandle = IntPtr.Zero;
            _startButtonHandle = IntPtr.Zero;
            HideTaskbar();
        }
    }
}
