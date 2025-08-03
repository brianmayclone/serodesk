using System.Runtime.InteropServices;

namespace SeroDesk.Platform
{
    public static class WindowsIntegration
    {
        private static readonly List<Action> _cleanupActions = new();
        
        public static void Initialize()
        {
            // Set process DPI awareness
            SetProcessDpiAwareness();
        }
        
        public static void SetWindowExTransparent(IntPtr hwnd)
        {
            int extendedStyle = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);
            NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE, 
                extendedStyle | NativeMethods.WS_EX_TRANSPARENT | NativeMethods.WS_EX_LAYERED);
        }
        
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
        
        public static void ExtendFrameIntoClientArea(IntPtr hwnd)
        {
            var margins = new NativeMethods.MARGINS { Left = -1, Right = -1, Top = -1, Bottom = -1 };
            NativeMethods.DwmExtendFrameIntoClientArea(hwnd, ref margins);
        }
        
        public static void SetWindowAlwaysOnTop(IntPtr hwnd)
        {
            // Force window to be always on top
            NativeMethods.SetWindowPos(hwnd, (IntPtr)NativeMethods.HWND_TOPMOST, 0, 0, 0, 0, 
                NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_SHOWWINDOW);
                
            // Also bring to foreground and set focus
            NativeMethods.BringWindowToTop(hwnd);
            NativeMethods.SetForegroundWindow(hwnd);
        }
        
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