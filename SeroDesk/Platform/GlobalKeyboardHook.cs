using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace SeroDesk.Platform
{
    public class GlobalKeyboardHook : IDisposable
    {
        private IntPtr _hookID = IntPtr.Zero;
        private NativeMethods.LowLevelKeyboardProc _proc; // Keep strong reference to prevent GC
        private bool _disposed = false;
        private static GlobalKeyboardHook? _instance;
        
        public event EventHandler? WindowsKeyPressed;
        
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
