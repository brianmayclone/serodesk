using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace SeroDesk.Platform
{
    /// <summary>
    /// Provides system-wide keyboard hook functionality to intercept Windows key presses for shell integration.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The GlobalKeyboardHook class implements a low-level keyboard hook to capture Windows key presses
    /// before they reach the Windows shell. This is essential for SeroDesk's shell replacement functionality:
    /// <list type="bullet">
    /// <item>Intercepts Windows key presses to show custom Start menu/LaunchPad</item>
    /// <item>Prevents the default Windows Start menu from appearing</item>
    /// <item>Provides system-wide hotkey functionality</item>
    /// <item>Maintains proper hook lifecycle to prevent system issues</item>
    /// </list>
    /// </para>
    /// <para>
    /// The class uses Windows' low-level keyboard hook (WH_KEYBOARD_LL) which operates at a system level
    /// and can intercept keystrokes before they reach any application. This requires careful resource
    /// management to prevent system instability.
    /// </para>
    /// <para>
    /// <strong>IMPORTANT:</strong> This class must be properly disposed to unhook the keyboard hook
    /// and prevent system performance degradation or instability.
    /// </para>
    /// </remarks>
    public class GlobalKeyboardHook : IDisposable
    {
        /// <summary>
        /// Handle to the installed keyboard hook.
        /// </summary>
        private IntPtr _hookID = IntPtr.Zero;
        
        /// <summary>
        /// Strong reference to the hook procedure to prevent garbage collection.
        /// </summary>
        /// <remarks>
        /// This field maintains a reference to the callback delegate to prevent it from being
        /// garbage collected while the hook is active, which would cause system crashes.
        /// </remarks>
        private NativeMethods.LowLevelKeyboardProc _proc;
        
        /// <summary>
        /// Indicates whether this instance has been disposed.
        /// </summary>
        private bool _disposed = false;
        
        /// <summary>
        /// Static reference to the current instance for callback access.
        /// </summary>
        private static GlobalKeyboardHook? _instance;
        
        /// <summary>
        /// Occurs when the Windows key is pressed while the hook is active.
        /// </summary>
        /// <remarks>
        /// This event is raised when either the left or right Windows key is pressed,
        /// allowing SeroDesk to respond by showing the LaunchPad or other shell interfaces.
        /// </remarks>
        public event EventHandler? WindowsKeyPressed;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="GlobalKeyboardHook"/> class and installs the keyboard hook.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The constructor performs the following operations:
        /// <list type="number">
        /// <item>Sets up the static instance reference for callback access</item>
        /// <item>Creates a strong reference to the hook callback procedure</item>
        /// <item>Installs the low-level keyboard hook using Windows APIs</item>
        /// <item>Validates the hook installation and logs any errors</item>
        /// </list>
        /// </para>
        /// <para>
        /// If the hook installation fails, the instance will still be created but will not
        /// intercept keyboard events. Error information is logged for debugging purposes.
        /// </para>
        /// </remarks>
        public GlobalKeyboardHook()
        {
            _instance = this;
            _proc = HookCallback; // Assign to instance field to prevent GC
            _hookID = SetHook(_proc);
            
            // Check if hook was installed successfully
            if (_hookID == IntPtr.Zero)
            {
                var error = Marshal.GetLastWin32Error();
                System.Diagnostics.Debug.WriteLine($"Failed to install keyboard hook. Win32 Error: {error}");
            }
        }
        
        private static IntPtr SetHook(NativeMethods.LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule? curModule = curProcess.MainModule)
            {
                if (curModule != null)
                {
                    return NativeMethods.SetWindowsHookEx(
                        NativeMethods.WH_KEYBOARD_LL,
                        proc,
                        NativeMethods.GetModuleHandle(curModule.ModuleName),
                        0);
                }
            }
            return IntPtr.Zero;
        }
        
        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                if (wParam == (IntPtr)NativeMethods.WM_KEYDOWN || wParam == (IntPtr)NativeMethods.WM_SYSKEYDOWN)
                {
                    var hookStruct = Marshal.PtrToStructure<NativeMethods.KBDLLHOOKSTRUCT>(lParam);
                    
                    // Check for Windows key press
                    if (hookStruct.vkCode == NativeMethods.VK_LWIN || hookStruct.vkCode == NativeMethods.VK_RWIN)
                    {
                        // Trigger the event on the current instance
                        _instance?.OnWindowsKeyPressed();
                        
                        // Suppress the Windows key to prevent Start Menu from opening
                        return (IntPtr)1;
                    }
                }
            }
            
            return NativeMethods.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }
        
        private void OnWindowsKeyPressed()
        {
            WindowsKeyPressed?.Invoke(this, EventArgs.Empty);
        }
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                if (_hookID != IntPtr.Zero)
                {
                    NativeMethods.UnhookWindowsHookEx(_hookID);
                    _hookID = IntPtr.Zero;
                }
                _instance = null;
                _disposed = true;
            }
        }
        
        ~GlobalKeyboardHook()
        {
            Dispose(false);
        }
    }
}
