using System.Runtime.InteropServices;

namespace SeroDesk.Platform
{
    /// <summary>
    /// Manages Windows taskbar visibility and behavior
    /// </summary>
    public static class TaskbarManager
    {
        private static IntPtr _taskbarHandle = IntPtr.Zero;
        private static IntPtr _startButtonHandle = IntPtr.Zero;
        private static bool _isTaskbarHidden = false;
        
        /// <summary>
        /// Gets the handle to the Windows taskbar
        /// </summary>
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
        /// Gets the handle to the Start button
        /// </summary>
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
        /// Completely hides the Windows taskbar
        /// </summary>
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
        /// Hides secondary taskbars on multi-monitor systems
        /// </summary>
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
        /// Gets the class name of a window
        /// </summary>
        private static string GetWindowClassName(IntPtr hWnd)
        {
            var className = new System.Text.StringBuilder(256);
            NativeMethods.GetClassName(hWnd, className, className.Capacity);
            return className.ToString();
        }
        
        /// <summary>
        /// Shows the Windows taskbar
        /// </summary>
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
        /// Shows secondary taskbars on multi-monitor systems
        /// </summary>
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
        /// Gets whether the taskbar is currently hidden
        /// </summary>
        public static bool IsTaskbarHidden => _isTaskbarHidden;
        
        /// <summary>
        /// Forces a complete taskbar refresh and rehiding
        /// </summary>
        public static void ForceHideTaskbar()
        {
            _isTaskbarHidden = false;
            _taskbarHandle = IntPtr.Zero;
            _startButtonHandle = IntPtr.Zero;
            HideTaskbar();
        }
    }
}
