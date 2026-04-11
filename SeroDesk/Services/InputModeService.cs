using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace SeroDesk.Services
{
    /// <summary>
    /// Detects input mode changes between touch, mouse, and keyboard.
    /// Automatically detects whether a physical keyboard is attached and adjusts UI accordingly.
    /// </summary>
    public class InputModeService : INotifyPropertyChanged
    {
        private static InputModeService? _instance;
        public static InputModeService Instance => _instance ??= new InputModeService();

        private readonly DispatcherTimer _detectionTimer;
        private bool _isKeyboardAttached;
        private bool _isTouchDevice;
        private InputMode _currentMode = InputMode.Touch;

        /// <summary>
        /// Whether a physical keyboard is currently connected.
        /// </summary>
        public bool IsKeyboardAttached
        {
            get => _isKeyboardAttached;
            private set
            {
                if (_isKeyboardAttached != value)
                {
                    _isKeyboardAttached = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ShouldShowOnScreenKeyboard));
                    Logger.Info($"Keyboard attached: {value}");
                }
            }
        }

        /// <summary>
        /// Whether the device has a touch screen.
        /// </summary>
        public bool IsTouchDevice
        {
            get => _isTouchDevice;
            private set
            {
                if (_isTouchDevice != value)
                {
                    _isTouchDevice = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Current primary input mode (Touch, Mouse, or Keyboard).
        /// </summary>
        public InputMode CurrentMode
        {
            get => _currentMode;
            private set
            {
                if (_currentMode != value)
                {
                    _currentMode = value;
                    OnPropertyChanged();
                    InputModeChanged?.Invoke(this, value);
                }
            }
        }

        /// <summary>
        /// Whether the on-screen keyboard should be shown for text input.
        /// True when no physical keyboard is attached and device has touch.
        /// </summary>
        public bool ShouldShowOnScreenKeyboard => !_isKeyboardAttached && _isTouchDevice;

        /// <summary>
        /// Minimum touch target size in DIPs for the current input mode.
        /// Returns 44 for touch (iOS standard), 32 for mouse.
        /// </summary>
        public double MinTouchTargetSize => _currentMode == InputMode.Touch ? 44.0 : 32.0;

        /// <summary>
        /// Fired when the input mode changes.
        /// </summary>
        public event EventHandler<InputMode>? InputModeChanged;

        private InputModeService()
        {
            DetectInputDevices();

            // Recheck periodically (keyboards can be plugged/unplugged)
            _detectionTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            _detectionTimer.Tick += (s, e) => DetectInputDevices();
            _detectionTimer.Start();
        }

        /// <summary>
        /// Detects attached input devices and updates state.
        /// </summary>
        private void DetectInputDevices()
        {
            try
            {
                // Detect touch capability
                IsTouchDevice = GetSystemMetrics(SM_MAXIMUMTOUCHES) > 0;

                // Detect physical keyboard
                // SM_CONVERTIBLESLATEMODE returns 0 when in slate/tablet mode (no keyboard)
                // and 1 when in laptop/desktop mode (keyboard available)
                int slateMode = GetSystemMetrics(SM_CONVERTIBLESLATEMODE);
                bool hasKeyboard = slateMode != 0;

                // Also check for number of keyboards via raw input device list
                if (!hasKeyboard)
                {
                    hasKeyboard = CheckForPhysicalKeyboard();
                }

                IsKeyboardAttached = hasKeyboard;

                // Determine input mode
                if (IsTouchDevice && !IsKeyboardAttached)
                    CurrentMode = InputMode.Touch;
                else if (IsTouchDevice && IsKeyboardAttached)
                    CurrentMode = InputMode.Touch; // Prefer touch on convertibles
                else
                    CurrentMode = InputMode.Mouse;
            }
            catch (Exception ex)
            {
                Logger.Error("Input device detection failed", ex);
                // Default to touch-friendly mode
                IsTouchDevice = true;
                IsKeyboardAttached = true;
                CurrentMode = InputMode.Touch;
            }
        }

        private bool CheckForPhysicalKeyboard()
        {
            try
            {
                uint deviceCount = 0;
                uint structSize = (uint)Marshal.SizeOf<RAWINPUTDEVICELIST>();
                GetRawInputDeviceList(IntPtr.Zero, ref deviceCount, structSize);

                if (deviceCount == 0) return false;

                var devices = new RAWINPUTDEVICELIST[deviceCount];
                var pDevices = Marshal.AllocHGlobal((int)(structSize * deviceCount));
                try
                {
                    GetRawInputDeviceList(pDevices, ref deviceCount, structSize);
                    for (uint i = 0; i < deviceCount; i++)
                    {
                        var device = Marshal.PtrToStructure<RAWINPUTDEVICELIST>(
                            IntPtr.Add(pDevices, (int)(i * structSize)));
                        if (device.dwType == RIM_TYPEKEYBOARD)
                            return true;
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(pDevices);
                }
            }
            catch { }
            return false;
        }

        #region P/Invoke

        private const int SM_MAXIMUMTOUCHES = 95;
        private const int SM_CONVERTIBLESLATEMODE = 0x2003;
        private const int RIM_TYPEKEYBOARD = 1;

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll")]
        private static extern uint GetRawInputDeviceList(IntPtr pRawInputDeviceList,
            ref uint puiNumDevices, uint cbSize);

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUTDEVICELIST
        {
            public IntPtr hDevice;
            public uint dwType;
        }

        #endregion

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public enum InputMode
    {
        Touch,
        Mouse,
        Keyboard
    }
}
