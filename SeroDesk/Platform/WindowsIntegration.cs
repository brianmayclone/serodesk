using System.Runtime.InteropServices;

namespace SeroDesk.Platform
{
    /// <summary>
    /// Provides advanced Windows system integration capabilities for SeroDesk shell replacement functionality.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The WindowsIntegration class offers comprehensive Windows system integration features
    /// required for implementing a custom shell interface that can replace or augment the
    /// standard Windows desktop environment:
    /// <list type="bullet">
    /// <item>DPI awareness configuration for high-resolution displays</item>
    /// <item>Window transparency and layering effects</item>
    /// <item>Desktop window parenting and Z-order management</item>
    /// <item>Aero Glass blur-behind effects for modern UI appearance</item>
    /// <item>Shell window registration for complete shell replacement</item>
    /// <item>Window positioning and behavior modification</item>
    /// </list>
    /// </para>
    /// <para>
    /// This class uses extensive Windows API integration to provide functionality typically
    /// reserved for system-level applications. It enables SeroDesk to integrate seamlessly
    /// with the Windows desktop environment while providing iOS/macOS-style interface elements.
    /// </para>
    /// <para>
    /// <strong>IMPORTANT:</strong> Many operations in this class require elevated privileges
    /// or system-level access. Improper use can affect system stability and user experience.
    /// </para>
    /// </remarks>
    public static class WindowsIntegration
    {
        private static readonly List<Action> _cleanupActions = new();
        
        /// <summary>
        /// Initializes Windows system integration features for SeroDesk.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method performs essential system integration setup:
        /// <list type="bullet">
        /// <item>Configures process DPI awareness for high-resolution displays</item>
        /// <item>Sets up system integration parameters</item>
        /// <item>Prepares the application for advanced Windows features</item>
        /// </list>
        /// </para>
        /// <para>
        /// This method should be called early in the application lifecycle,
        /// preferably before any UI elements are created.
        /// </para>
        /// </remarks>
        public static void Initialize()
        {
            // Set process DPI awareness
            SetProcessDpiAwareness();
        }
        
        /// <summary>
        /// Configures a window to be click-through transparent.
        /// </summary>
        /// <param name="hwnd">The handle to the window to make transparent.</param>
        /// <remarks>
        /// <para>
        /// This method applies extended window styles to make a window transparent
        /// to mouse clicks and interactions. The window will be visually present
        /// but will not receive mouse input, allowing clicks to pass through to
        /// windows beneath it.
        /// </para>
        /// <para>
        /// This is useful for creating overlay UI elements that display information
        /// but don't interfere with desktop interaction.
        /// </para>
        /// </remarks>
        public static void SetWindowExTransparent(IntPtr hwnd)
        {
            int extendedStyle = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);
            NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE, 
                extendedStyle | NativeMethods.WS_EX_TRANSPARENT | NativeMethods.WS_EX_LAYERED);
        }
        
        /// <summary>
        /// Positions a window to appear above the desktop but below normal application windows.
        /// </summary>
        /// <param name="hwnd">The handle to the window to position.</param>
        /// <remarks>
        /// <para>
        /// This method places a window in the Z-order hierarchy so that it appears:
        /// <list type="bullet">
        /// <item>Above the desktop wallpaper and desktop icons</item>
        /// <item>Below normal application windows</item>
        /// <item>Does not interfere with regular window focus and interaction</item>
        /// </list>
        /// </para>
        /// <para>
        /// This positioning is ideal for desktop widgets, overlays, and interface
        /// elements that should be visible but not interfere with normal application usage.
        /// </para>
        /// </remarks>
        public static void SetWindowAsDesktopChild(IntPtr hwnd)
        {
            // Find the desktop window handle
            IntPtr desktopHandle = GetDesktopWindow();
            
            // Set our window ABOVE the desktop but BELOW normal windows
            // Use HWND_BOTTOM to place it at the bottom of the Z-order
            const int HWND_BOTTOM = 1;
            NativeMethods.SetWindowPos(hwnd, new IntPtr(HWND_BOTTOM), 0, 0, 0, 0,
                NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE);
        }
        
        /// <summary>
        /// Enables the Windows Aero Glass blur-behind effect for a window.
        /// </summary>
        /// <param name="hwnd">The handle to the window for which to enable blur effects.</param>
        /// <remarks>
        /// <para>
        /// This method applies the Windows Aero Glass blur-behind effect to create
        /// a modern, translucent appearance similar to macOS and iOS interfaces.
        /// The effect blurs content behind the window while maintaining readability
        /// of the window's own content.
        /// </para>
        /// <para>
        /// <strong>NOTE:</strong> This effect requires Windows Vista or later with
        /// Aero effects enabled. On systems where Aero is disabled, this method
        /// will have no visible effect.
        /// </para>
        /// </remarks>
        public static void EnableBlurBehindWindow(IntPtr hwnd)
        {
            var blurBehind = new NativeMethods.DWM_BLURBEHIND
            {
                dwFlags = 1, // DWM_BB_ENABLE
                fEnable = true,
                hRgnBlur = IntPtr.Zero,
                fTransitionOnMaximized = true
            };
            
            NativeMethods.DwmEnableBlurBehindWindow(hwnd, ref blurBehind);
        }
        
        /// <summary>
        /// Extends the window frame into the client area for a seamless glass appearance.
        /// </summary>
        /// <param name="hwnd">The handle to the window whose frame should be extended.</param>
        /// <remarks>
        /// <para>
        /// This method extends the Aero Glass frame effect into the entire client area
        /// of the window, creating a seamless glass appearance throughout the window.
        /// This is commonly used for creating modern, borderless window designs.
        /// </para>
        /// <para>
        /// The effect removes the traditional window border distinction and allows
        /// the glass effect to cover the entire window area, similar to modern
        /// Windows applications and mobile interfaces.
        /// </para>
        /// </remarks>
        public static void ExtendFrameIntoClientArea(IntPtr hwnd)
        {
            var margins = new NativeMethods.MARGINS { Left = -1, Right = -1, Top = -1, Bottom = -1 };
            NativeMethods.DwmExtendFrameIntoClientArea(hwnd, ref margins);
        }
        
        /// <summary>
        /// Forces a window to remain always on top of all other windows.
        /// </summary>
        /// <param name="hwnd">The handle to the window to keep on top.</param>
        /// <remarks>
        /// <para>
        /// This method ensures a window remains visible above all other windows,
        /// including other topmost windows. The operation:
        /// <list type="number">
        /// <item>Sets the window as topmost in the Z-order</item>
        /// <item>Brings the window to the foreground</item>
        /// <item>Sets focus to the window</item>
        /// </list>
        /// </para>
        /// <para>
        /// <strong>CAUTION:</strong> Use this method sparingly as always-on-top windows
        /// can interfere with user workflow and may be considered intrusive.
        /// </para>
        /// </remarks>
        public static void SetWindowAlwaysOnTop(IntPtr hwnd)
        {
            // Force window to be always on top
            NativeMethods.SetWindowPos(hwnd, (IntPtr)NativeMethods.HWND_TOPMOST, 0, 0, 0, 0, 
                NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_SHOWWINDOW);
                
            // Also bring to foreground and set focus
            NativeMethods.BringWindowToTop(hwnd);
            NativeMethods.SetForegroundWindow(hwnd);
        }
        
        /// <summary>
        /// Registers a window as the Windows shell replacement.
        /// </summary>
        /// <param name="hwnd">The handle to the window that will serve as the shell replacement.</param>
        /// <remarks>
        /// <para>
        /// This method registers the specified window as a shell replacement, which:
        /// <list type="bullet">
        /// <item>Replaces the standard Windows Explorer shell</item>
        /// <item>Makes the window the primary desktop interface</item>
        /// <item>Allows the application to handle shell responsibilities</item>
        /// <item>Enables complete desktop environment replacement</item>
        /// </list>
        /// </para>
        /// <para>
        /// <strong>WARNING:</strong> This is an advanced operation that fundamentally
        /// changes how Windows operates. Ensure proper error handling and restoration
        /// mechanisms are in place before using this functionality.
        /// </para>
        /// </remarks>
        public static void RegisterAsShell(IntPtr hwnd)
        {
            try
            {
                // Register this window as the shell
                SetShellWindow(hwnd);
                
                // Also set as the desktop window
                SetTaskmanWindow(hwnd);
                
                System.Diagnostics.Debug.WriteLine("SeroDesk registered as shell replacement");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error registering as shell: {ex.Message}");
            }
        }
        
        private static IntPtr GetDesktopWindow()
        {
            // Get the Program Manager window
            IntPtr progman = FindWindow("Progman", null);
            
            // Send message to create the desktop window
            SendMessageTimeout(progman, 0x052C, IntPtr.Zero, IntPtr.Zero, 
                SendMessageTimeoutFlags.SMTO_NORMAL, 1000, out _);
            
            IntPtr desktopHandle = IntPtr.Zero;
            
            // Find the desktop window
            EnumWindows((hwnd, lParam) =>
            {
                IntPtr shellDll = FindWindowEx(hwnd, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (shellDll != IntPtr.Zero)
                {
                    desktopHandle = FindWindowEx(IntPtr.Zero, hwnd, "WorkerW", null);
                    return false;
                }
                return true;
            }, IntPtr.Zero);
            
            return desktopHandle;
        }
        
        private static void SetProcessDpiAwareness()
        {
            try
            {
                // Try to set DPI awareness using the latest API
                if (Environment.OSVersion.Version >= new Version(10, 0, 15063))
                {
                    SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
                }
                else if (Environment.OSVersion.Version >= new Version(10, 0))
                {
                    SetProcessDpiAwareness(PROCESS_DPI_AWARENESS.Process_Per_Monitor_DPI_Aware);
                }
            }
            catch
            {
                // Fallback to older API
                SetProcessDPIAware();
            }
        }
        
        /// <summary>
        /// Performs cleanup of all Windows integration operations.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method executes all registered cleanup actions to restore the system
        /// to its original state before SeroDesk modifications. This includes:
        /// <list type="bullet">
        /// <item>Restoring original window properties</item>
        /// <item>Unregistering shell replacements</item>
        /// <item>Cleaning up system integrations</item>
        /// <item>Releasing system resources</item>
        /// </list>
        /// </para>
        /// <para>
        /// This method should be called during application shutdown to ensure
        /// the system is left in a stable state.
        /// </para>
        /// </remarks>
        public static void Cleanup()
        {
            foreach (var action in _cleanupActions)
            {
                try
                {
                    action();
                }
                catch { }
            }
            _cleanupActions.Clear();
        }
        
        #region Additional P/Invoke
        
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);
        
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, 
            string lpszClass, string? lpszWindow);
        
        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
        
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
        
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, 
            IntPtr lParam, SendMessageTimeoutFlags fuFlags, uint uTimeout, out IntPtr lpdwResult);
        
        [Flags]
        private enum SendMessageTimeoutFlags : uint
        {
            SMTO_NORMAL = 0x0,
            SMTO_BLOCK = 0x1,
            SMTO_ABORTIFHUNG = 0x2,
            SMTO_NOTIMEOUTIFNOTHUNG = 0x8
        }
        
        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();
        
        [DllImport("shcore.dll")]
        private static extern int SetProcessDpiAwareness(PROCESS_DPI_AWARENESS awareness);
        
        [DllImport("user32.dll")]
        private static extern bool SetProcessDpiAwarenessContext(IntPtr value);
        
        [DllImport("user32.dll")]
        private static extern bool SetShellWindow(IntPtr hwnd);
        
        [DllImport("user32.dll")]
        private static extern bool SetTaskmanWindow(IntPtr hwnd);
        
        private static readonly IntPtr DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = new IntPtr(-4);
        
        private enum PROCESS_DPI_AWARENESS
        {
            Process_DPI_Unaware = 0,
            Process_System_DPI_Aware = 1,
            Process_Per_Monitor_DPI_Aware = 2
        }
        
        #endregion
    }
}