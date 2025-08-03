using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace SeroDesk.Platform
{
    public class GlobalKeyboardHook : IDisposable
    {
        private IntPtr _hookID = IntPtr.Zero;
        private NativeMethods.LowLevelKeyboardProc _proc = HookCallback;
        private bool _disposed = false;
        
        public event EventHandler? WindowsKeyPressed;
        
        public GlobalKeyboardHook()
        {
            _hookID = SetHook(_proc);
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
                        // Get the instance and trigger the event
                        var instance = App.Current?.FindResource("GlobalKeyboardHook") as GlobalKeyboardHook;
                        instance?.OnWindowsKeyPressed();
                        
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
                _disposed = true;
            }
        }
        
        ~GlobalKeyboardHook()
        {
            Dispose(false);
        }
    }
}
