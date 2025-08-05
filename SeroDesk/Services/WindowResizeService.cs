using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using SeroDesk.Constants;

namespace SeroDesk.Services
{
    /// <summary>
    /// Provides automatic window resizing functionality when the StatusBar is expanded or collapsed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This service monitors all visible windows and automatically adjusts their position and size
    /// to prevent them from being hidden behind the expanded StatusBar. When the StatusBar is collapsed,
    /// windows are restored to their original positions.
    /// </para>
    /// <para>
    /// The service uses Windows API calls to enumerate and manipulate window positions while maintaining
    /// the original window state for accurate restoration.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Usage example
    /// var resizeService = WindowResizeService.Instance;
    /// resizeService.OnStatusBarToggled(true, 60); // StatusBar expanded to 60 pixels
    /// resizeService.OnStatusBarToggled(false); // StatusBar collapsed
    /// </code>
    /// </example>
    public class WindowResizeService
    {
        private static WindowResizeService? _instance;
        
        /// <summary>
        /// Gets the singleton instance of the WindowResizeService.
        /// </summary>
        /// <value>The singleton instance.</value>
        public static WindowResizeService Instance => _instance ??= new WindowResizeService();
        
        /// <summary>
        /// Dictionary tracking the original state of windows that have been adjusted.
        /// </summary>
        private readonly Dictionary<IntPtr, WindowState> _trackedWindows = new();
        
        /// <summary>
        /// Indicates whether the StatusBar is currently expanded.
        /// </summary>
        private bool _statusBarExpanded = false;
        
        /// <summary>
        /// The current height of the StatusBar in pixels.
        /// </summary>
        private double _statusBarHeight = UIConstants.DefaultStatusBarHeight;
        
        /// <summary>
        /// Represents the stored state of a window for restoration purposes.
        /// </summary>
        private class WindowState
        {
            /// <summary>
            /// Gets or sets the original rectangle of the window before adjustment.
            /// </summary>
            public RECT OriginalRect { get; set; }
            
            /// <summary>
            /// Gets or sets a value indicating whether the window has been adjusted for the StatusBar.
            /// </summary>
            public bool IsAdjusted { get; set; }
        }
        
        /// <summary>
        /// Represents a rectangle structure used by Windows API.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            /// <summary>
            /// The x-coordinate of the upper-left corner of the rectangle.
            /// </summary>
            public int Left;
            
            /// <summary>
            /// The y-coordinate of the upper-left corner of the rectangle.
            /// </summary>
            public int Top;
            
            /// <summary>
            /// The x-coordinate of the lower-right corner of the rectangle.
            /// </summary>
            public int Right;
            
            /// <summary>
            /// The y-coordinate of the lower-right corner of the rectangle.
            /// </summary>
            public int Bottom;
            
            /// <summary>
            /// Gets the width of the rectangle.
            /// </summary>
            public int Width => Right - Left;
            
            /// <summary>
            /// Gets the height of the rectangle.
            /// </summary>
            public int Height => Bottom - Top;
        }
        
        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);
        
        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        
        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);
        
        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);
        
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);
        
        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        
        /// <summary>
        /// SetWindowPos flag: Retains the current Z order (ignores the hWndInsertAfter parameter).
        /// </summary>
        private const uint SWP_NOZORDER = 0x0004;
        
        /// <summary>
        /// SetWindowPos flag: Does not activate the window.
        /// </summary>
        private const uint SWP_NOACTIVATE = 0x0010;
        
        private WindowResizeService()
        {
            System.Diagnostics.Debug.WriteLine("WindowResizeService initialized");
        }
        
        /// <summary>
        /// Called when the StatusBar is expanded or collapsed to trigger window adjustments.
        /// </summary>
        /// <param name="isExpanded">True if the StatusBar is expanded; false if collapsed.</param>
        /// <param name="statusBarHeight">The height of the expanded StatusBar in pixels. Defaults to <see cref="UIConstants.DefaultStatusBarHeight"/>.</param>
        /// <remarks>
        /// When the StatusBar is expanded, all windows that would be overlapped are moved down and resized.
        /// When collapsed, all adjusted windows are restored to their original positions.
        /// </remarks>
        public void OnStatusBarToggled(bool isExpanded, double statusBarHeight = UIConstants.DefaultStatusBarHeight)
        {
            System.Diagnostics.Debug.WriteLine($"StatusBar toggled - Expanded: {isExpanded}, Height: {statusBarHeight}");
            
            _statusBarExpanded = isExpanded;
            _statusBarHeight = statusBarHeight;
            
            if (isExpanded)
            {
                AdjustWindowsForStatusBar();
            }
            else
            {
                RestoreOriginalWindowSizes();
            }
        }
        
        /// <summary>
        /// Adjusts all visible windows to accommodate the expanded StatusBar.
        /// </summary>
        /// <remarks>
        /// This method enumerates all visible windows, saves their current state if not already tracked,
        /// and adjusts their position and size to ensure they remain fully visible below the StatusBar.
        /// </remarks>
        private void AdjustWindowsForStatusBar()
        {
            System.Diagnostics.Debug.WriteLine("Adjusting windows for expanded StatusBar");
            
            // Find and adjust all visible windows
            var visibleWindows = GetVisibleWindows();
            
            foreach (var hwnd in visibleWindows)
            {
                try
                {
                    // Save current state if not already saved
                    if (!_trackedWindows.ContainsKey(hwnd))
                    {
                        if (GetWindowRect(hwnd, out RECT rect))
                        {
                            _trackedWindows[hwnd] = new WindowState
                            {
                                OriginalRect = rect,
                                IsAdjusted = false
                            };
                        }
                    }
                    
                    var windowState = _trackedWindows[hwnd];
                    if (!windowState.IsAdjusted)
                    {
                        // Adjust window
                        AdjustSingleWindow(hwnd, windowState);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error adjusting window {hwnd}: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// Adjusts the position and size of a single window to accommodate the StatusBar.
        /// </summary>
        /// <param name="hwnd">The handle to the window to adjust.</param>
        /// <param name="windowState">The tracked state of the window containing its original position.</param>
        /// <remarks>
        /// Only windows whose top edge would be covered by the StatusBar are adjusted.
        /// The window is moved down to start at the StatusBar's bottom edge and its height is reduced accordingly.
        /// Windows that would become too small (less than <see cref="UIConstants.MinimumWindowHeight"/>) are not adjusted.
        /// </remarks>
        private void AdjustSingleWindow(IntPtr hwnd, WindowState windowState)
        {
            var original = windowState.OriginalRect;
            
            // Check if window is overlapped by StatusBar
            if (original.Top < _statusBarHeight)
            {
                System.Diagnostics.Debug.WriteLine($"Adjusting window at top {original.Top} (StatusBar height: {_statusBarHeight})");
                
                // Calculate new position and size
                int newTop = (int)_statusBarHeight;
                int newHeight = original.Height - (newTop - original.Top);
                
                // Ensure window remains visible
                if (newHeight > UIConstants.MinimumWindowHeight)
                {
                    bool success = SetWindowPos(hwnd, IntPtr.Zero, 
                        original.Left, newTop, 
                        original.Width, newHeight, 
                        SWP_NOZORDER | SWP_NOACTIVATE);
                    
                    if (success)
                    {
                        windowState.IsAdjusted = true;
                        System.Diagnostics.Debug.WriteLine($"Successfully adjusted window to {original.Left},{newTop} {original.Width}x{newHeight}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Failed to adjust window position");
                    }
                }
            }
        }
        
        /// <summary>
        /// Restores all adjusted windows to their original sizes and positions.
        /// </summary>
        /// <remarks>
        /// This method iterates through all tracked windows and restores those that have been adjusted.
        /// After restoration, it performs cleanup to remove windows that no longer exist.
        /// </remarks>
        private void RestoreOriginalWindowSizes()
        {
            System.Diagnostics.Debug.WriteLine("Restoring original window sizes");
            
            foreach (var kvp in _trackedWindows)
            {
                var hwnd = kvp.Key;
                var windowState = kvp.Value;
                
                if (windowState.IsAdjusted)
                {
                    try
                    {
                        var original = windowState.OriginalRect;
                        
                        bool success = SetWindowPos(hwnd, IntPtr.Zero,
                            original.Left, original.Top,
                            original.Width, original.Height,
                            SWP_NOZORDER | SWP_NOACTIVATE);
                        
                        if (success)
                        {
                            windowState.IsAdjusted = false;
                            System.Diagnostics.Debug.WriteLine($"Successfully restored window to {original.Left},{original.Top} {original.Width}x{original.Height}");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error restoring window {hwnd}: {ex.Message}");
                    }
                }
            }
            
            // Cleanup - remove no longer existing windows
            CleanupTrackedWindows();
        }
        
        /// <summary>
        /// Finds all visible windows that should be considered for adjustment.
        /// </summary>
        /// <returns>A list of window handles for all visible, non-minimized windows.</returns>
        /// <remarks>
        /// This method filters out:
        /// <list type="bullet">
        /// <item>Windows belonging to the SeroDesk process itself</item>
        /// <item>System windows like Program Manager and Desktop Window Manager</item>
        /// <item>Minimized windows</item>
        /// <item>Windows without titles</item>
        /// </list>
        /// </remarks>
        private List<IntPtr> GetVisibleWindows()
        {
            var windows = new List<IntPtr>();
            var currentProcessId = (uint)Process.GetCurrentProcess().Id;
            
            // Enumerate all top-level windows 
            SeroDesk.Platform.NativeMethods.EnumWindows((hwnd, lParam) =>
            {
                try
                {
                    // Only visible, non-minimized windows
                    if (IsWindowVisible(hwnd) && !IsIconic(hwnd))
                    {
                        // Exclude own windows (SeroDesk)
                        GetWindowThreadProcessId(hwnd, out uint windowProcessId);
                        if (windowProcessId != currentProcessId)
                        {
                            // Check title to filter system windows
                            var title = GetWindowTitle(hwnd);
                            if (!string.IsNullOrEmpty(title) && 
                                !title.Contains("Program Manager") && 
                                !title.Contains("Desktop Window Manager"))
                            {
                                windows.Add(hwnd);
                            }
                        }
                    }
                }
                catch
                {
                    // Ignore errors for individual windows
                }
                
                return true; // Continue enumeration
            }, IntPtr.Zero);
            
            System.Diagnostics.Debug.WriteLine($"Found {windows.Count} visible windows to potentially adjust");
            return windows;
        }
        
        /// <summary>
        /// Gets the title text of a window.
        /// </summary>
        /// <param name="hwnd">The handle to the window.</param>
        /// <returns>The window title, or an empty string if the title cannot be retrieved.</returns>
        private string GetWindowTitle(IntPtr hwnd)
        {
            var title = new System.Text.StringBuilder(256);
            GetWindowText(hwnd, title, title.Capacity);
            return title.ToString();
        }
        
        /// <summary>
        /// Removes windows that no longer exist from the tracking list.
        /// </summary>
        /// <remarks>
        /// This cleanup prevents memory leaks by removing references to windows that have been closed.
        /// It is called after restoring windows to their original positions.
        /// </remarks>
        private void CleanupTrackedWindows()
        {
            var toRemove = new List<IntPtr>();
            
            foreach (var hwnd in _trackedWindows.Keys)
            {
                if (!IsWindowVisible(hwnd))
                {
                    toRemove.Add(hwnd);
                }
            }
            
            foreach (var hwnd in toRemove)
            {
                _trackedWindows.Remove(hwnd);
            }
            
            if (toRemove.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"Cleaned up {toRemove.Count} no longer visible windows");
            }
        }
        
        /// <summary>
        /// Resets all window tracking and restores any adjusted windows to their original positions.
        /// </summary>
        /// <remarks>
        /// This method should be called during application shutdown to ensure all windows are
        /// properly restored before the service is disposed.
        /// </remarks>
        public void Reset()
        {
            System.Diagnostics.Debug.WriteLine("WindowResizeService reset - restoring all windows");
            
            if (_statusBarExpanded)
            {
                RestoreOriginalWindowSizes();
            }
            
            _trackedWindows.Clear();
            _statusBarExpanded = false;
        }
    }
}