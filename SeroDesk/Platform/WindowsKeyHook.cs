using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

namespace SeroDesk.Platform
{
    public class WindowsKeyHook : IDisposable
    {
        private const int WH_KEYBOARD_LL = 13;
        private NativeMethods.LowLevelKeyboardProc _proc = HookCallback;
        private IntPtr _hookID = IntPtr.Zero;
        private static WindowsKeyHook? _instance;
        private static MainWindow? _mainWindow;
        private static bool _isDisposed = false;
        private static bool _isProcessingWindowsKey = false;

        public static void Initialize(MainWindow mainWindow)
        {
            if (_instance == null)
            {
                _instance = new WindowsKeyHook();
                _mainWindow = mainWindow;
                _instance.SetHook();
            }
        }

        public static void Shutdown()
        {
            _instance?.Dispose();
            _instance = null;
            _mainWindow = null;
        }

        private void SetHook()
        {
            using (var curProcess = Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                if (curModule?.ModuleName != null)
                {
                    _hookID = NativeMethods.SetWindowsHookEx(
                        WH_KEYBOARD_LL,
                        _proc,
                        NativeMethods.GetModuleHandle(curModule.ModuleName),
                        0);
                }
            }
        }

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && !_isDisposed)
            {
                try
                {
                    if (wParam == (IntPtr)NativeMethods.WM_KEYDOWN || wParam == (IntPtr)NativeMethods.WM_SYSKEYDOWN)
                    {
                        int vkCode = Marshal.ReadInt32(lParam);
                        
                        // Check for Windows key (left or right)
                        if (vkCode == NativeMethods.VK_LWIN || vkCode == NativeMethods.VK_RWIN)
                        {
                            // Prevent multiple concurrent Windows key processing
                            if (_isProcessingWindowsKey)
                            {
                                return (IntPtr)1; // Suppress the key but don't process
                            }
                            
                            _isProcessingWindowsKey = true;
                            
                            // Handle Windows key press on UI thread
                            if (_mainWindow != null)
                            {
                                _mainWindow.Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    try
                                    {
                                        _mainWindow.ShowLaunchpad();
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine($"Error handling Windows key: {ex.Message}");
                                    }
                                    finally
                                    {
                                        // Reset the flag after processing
                                        _isProcessingWindowsKey = false;
                                    }
                                }), DispatcherPriority.Normal);
                            }
                            else
                            {
                                _isProcessingWindowsKey = false;
                            }
                            
                            // Suppress the Windows key to prevent Start Menu
                            return (IntPtr)1;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error in Windows key hook: {ex.Message}");
                }
            }

            return NativeMethods.CallNextHookEx(_instance?._hookID ?? IntPtr.Zero, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            _isDisposed = true;
            if (_hookID != IntPtr.Zero)
            {
                NativeMethods.UnhookWindowsHookEx(_hookID);
                _hookID = IntPtr.Zero;
            }
        }
    }
}