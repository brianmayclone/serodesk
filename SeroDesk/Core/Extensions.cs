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
    public static class Extensions
    {
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