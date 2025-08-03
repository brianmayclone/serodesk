using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace SeroDesk.Core
{
    /// <summary>
    /// Provides blur effects for WPF controls using Windows Desktop Window Manager (DWM) APIs.
    /// This implementation creates glassmorphism effects compatible with .NET 8.0 and Windows 11.
    /// </summary>
    /// <remarks>
    /// The BlurEffect class uses native Windows APIs to create blur effects behind UI elements:
    /// - DwmEnableBlurBehindWindow for window-level blur effects
    /// - Custom layered rendering for control-level blur simulation
    /// - Hardware-accelerated composition when available
    /// 
    /// This approach provides better performance and compatibility than third-party libraries
    /// while maintaining the modern glassmorphism aesthetic expected in Windows 11 applications.
    /// </remarks>
    public static class BlurEffect
    {
        #region Win32 API Declarations
        
        [DllImport("dwmapi.dll")]
        private static extern int DwmEnableBlurBehindWindow(IntPtr hwnd, ref DWM_BLURBEHIND blurBehind);
        
        [DllImport("user32.dll")]
        private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);
        
        [StructLayout(LayoutKind.Sequential)]
        private struct DWM_BLURBEHIND
        {
            public int dwFlags;
            public bool fEnable;
            public IntPtr hRgnBlur;
            public bool fTransitionOnMaximized;
        }
        
        [StructLayout(LayoutKind.Sequential)]
        private struct WindowCompositionAttributeData
        {
            public WindowCompositionAttribute Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }
        
        [StructLayout(LayoutKind.Sequential)]
        private struct AccentPolicy
        {
            public AccentState AccentState;
            public int AccentFlags;
            public int GradientColor;
            public int AnimationId;
        }
        
        private enum WindowCompositionAttribute
        {
            WCA_ACCENT_POLICY = 19
        }
        
        private enum AccentState
        {
            ACCENT_DISABLED = 0,
            ACCENT_ENABLE_GRADIENT = 1,
            ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
            ACCENT_ENABLE_BLURBEHIND = 3,
            ACCENT_ENABLE_ACRYLICBLURBEHIND = 4,
            ACCENT_INVALID_STATE = 5
        }
        
        #endregion
        
        /// <summary>
        /// Enables blur effect behind the specified window using Windows DWM APIs.
        /// This creates a system-level blur that works consistently across Windows 11.
        /// </summary>
        /// <param name="window">The WPF window to apply blur effect to</param>
        /// <param name="enable">Whether to enable or disable the blur effect</param>
        /// <returns>True if the blur effect was successfully applied, false otherwise</returns>
        public static bool EnableBlur(Window window, bool enable = true)
        {
            try
            {
                var windowHelper = new WindowInteropHelper(window);
                var hwnd = windowHelper.Handle;
                
                if (hwnd == IntPtr.Zero)
                    return false;
                
                // Try modern Windows 11 acrylic blur first
                if (TryEnableAcrylicBlur(hwnd, enable))
                    return true;
                
                // Fallback to traditional DWM blur
                return EnableDwmBlur(hwnd, enable);
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Attempts to enable Windows 11 acrylic blur effect using composition attributes.
        /// This provides the most modern and visually appealing blur effect available.
        /// </summary>
        /// <param name="hwnd">Window handle to apply the effect to</param>
        /// <param name="enable">Whether to enable the acrylic blur</param>
        /// <returns>True if acrylic blur was successfully enabled</returns>
        private static bool TryEnableAcrylicBlur(IntPtr hwnd, bool enable)
        {
            try
            {
                var accent = new AccentPolicy
                {
                    AccentState = enable ? AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND : AccentState.ACCENT_DISABLED,
                    AccentFlags = 2,
                    GradientColor = 0x01000000, // Subtle dark tint
                    AnimationId = 0
                };
                
                var accentStructSize = Marshal.SizeOf(accent);
                var accentPtr = Marshal.AllocHGlobal(accentStructSize);
                Marshal.StructureToPtr(accent, accentPtr, false);
                
                var data = new WindowCompositionAttributeData
                {
                    Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                    SizeOfData = accentStructSize,
                    Data = accentPtr
                };
                
                var result = SetWindowCompositionAttribute(hwnd, ref data);
                Marshal.FreeHGlobal(accentPtr);
                
                return result == 0; // 0 indicates success
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Enables traditional DWM blur as a fallback for older Windows versions
        /// or when acrylic blur is not available.
        /// </summary>
        /// <param name="hwnd">Window handle to apply the effect to</param>
        /// <param name="enable">Whether to enable the blur</param>
        /// <returns>True if DWM blur was successfully enabled</returns>
        private static bool EnableDwmBlur(IntPtr hwnd, bool enable)
        {
            try
            {
                var blurBehind = new DWM_BLURBEHIND
                {
                    dwFlags = 1, // DWM_BB_ENABLE
                    fEnable = enable,
                    hRgnBlur = IntPtr.Zero,
                    fTransitionOnMaximized = false
                };
                
                return DwmEnableBlurBehindWindow(hwnd, ref blurBehind) == 0;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Creates a glassmorphism background brush that simulates blur effects
        /// using gradient overlays. This works as a visual fallback when native
        /// blur APIs are not available or supported.
        /// </summary>
        /// <param name="opacity">Base opacity for the glass effect (0.0 to 1.0)</param>
        /// <param name="tintColor">Optional tint color for the glass effect</param>
        /// <returns>A brush that provides glassmorphism visual effects</returns>
        public static Brush CreateGlassmorphismBrush(double opacity = 0.3, Color? tintColor = null)
        {
            var baseColor = tintColor ?? Color.FromArgb(255, 255, 255, 255);
            var transparentColor = Color.FromArgb(0, baseColor.R, baseColor.G, baseColor.B);
            var semiTransparentColor = Color.FromArgb((byte)(255 * opacity), baseColor.R, baseColor.G, baseColor.B);
            
            var brush = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1)
            };
            
            brush.GradientStops.Add(new GradientStop(semiTransparentColor, 0.0));
            brush.GradientStops.Add(new GradientStop(transparentColor, 0.5));
            brush.GradientStops.Add(new GradientStop(semiTransparentColor, 1.0));
            
            return brush;
        }
        
        /// <summary>
        /// Creates a radial glassmorphism brush for circular or rounded elements.
        /// Provides a more sophisticated glass effect for icons and buttons.
        /// </summary>
        /// <param name="opacity">Base opacity for the glass effect</param>
        /// <param name="center">Center point of the radial effect</param>
        /// <returns>A radial brush for glassmorphism effects</returns>
        public static Brush CreateRadialGlassmorphismBrush(double opacity = 0.3, Point? center = null)
        {
            var centerPoint = center ?? new Point(0.5, 0.5);
            var whiteTransparent = Color.FromArgb((byte)(255 * opacity), 255, 255, 255);
            var whiteVeryTransparent = Color.FromArgb((byte)(255 * opacity * 0.3), 255, 255, 255);
            
            var brush = new RadialGradientBrush
            {
                Center = centerPoint,
                RadiusX = 0.8,
                RadiusY = 0.8
            };
            
            brush.GradientStops.Add(new GradientStop(whiteTransparent, 0.0));
            brush.GradientStops.Add(new GradientStop(whiteVeryTransparent, 0.7));
            brush.GradientStops.Add(new GradientStop(Colors.Transparent, 1.0));
            
            return brush;
        }
    }
}
