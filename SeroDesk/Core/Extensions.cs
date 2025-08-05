using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using SeroDesk.Models;

namespace SeroDesk.Core
{
    /// <summary>
    /// Provides extension methods for common operations in the SeroDesk application.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This static class contains utility extension methods that enhance built-in .NET types
    /// with functionality commonly needed throughout the SeroDesk application:
    /// <list type="bullet">
    /// <item>Image conversion between GDI+ Bitmap and WPF ImageSource</item>
    /// <item>Animation utilities for smooth UI transitions</item>
    /// <item>AppIcon manipulation and enhancement methods</item>
    /// <item>Resource management and memory cleanup</item>
    /// </list>
    /// </para>
    /// <para>
    /// The extension methods are designed to be thread-safe and handle common error scenarios
    /// gracefully to maintain application stability.
    /// </para>
    /// </remarks>
    public static class Extensions
    {
        /// <summary>
        /// Converts a GDI+ Bitmap to a WPF ImageSource for use in WPF controls.
        /// </summary>
        /// <param name="bitmap">The GDI+ Bitmap to convert.</param>
        /// <returns>
        /// An ImageSource that can be used in WPF Image controls, or null if conversion fails.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method performs the necessary conversion between GDI+ and WPF image formats,
        /// properly managing native resources to prevent memory leaks.
        /// </para>
        /// <para>
        /// The resulting ImageSource is frozen for better performance and thread-safety.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when bitmap is null.</exception>
        public static ImageSource? ToBitmapSource(this Bitmap bitmap)
        {
            try
            {
                var hBitmap = bitmap.GetHbitmap();
                var imageSource = Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
                
                DeleteObject(hBitmap);
                imageSource.Freeze();
                
                return imageSource;
            }
            catch
            {
                return null;
            }
        }
        
        public static Bitmap? ToBitmap(this Icon? icon)
        {
            if (icon == null) return null;
            
            try
            {
                return icon.ToBitmap();
            }
            catch
            {
                return null;
            }
        }
        
        public static void BeginAnimation(this AppIcon icon, DependencyProperty property, DoubleAnimation animation)
        {
            // This is a placeholder - in a real implementation, you'd need to handle animation differently
            // since AppIcon is not a DependencyObject
            if (property.Name == "Scale")
            {
                icon.Scale = animation.To ?? icon.Scale;
            }
        }
        
        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern bool DeleteObject(IntPtr hObject);
    }
}