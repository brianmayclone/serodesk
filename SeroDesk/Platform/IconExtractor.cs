using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SeroDesk.Platform
{
    public static class IconExtractor
    {
        private static readonly Dictionary<string, ImageSource> _iconCache = new();
        
        public static ImageSource? GetIconForFile(string filePath, bool largeIcon = true)
        {
            var cacheKey = $"{filePath}_{largeIcon}";
            
            if (_iconCache.TryGetValue(cacheKey, out var cached))
                return cached;
            
            try
            {
                var icon = ExtractIconFromFile(filePath, largeIcon);
                if (icon != null)
                {
                    var imageSource = ConvertIconToImageSource(icon);
                    if (imageSource != null)
                    {
                        _iconCache[cacheKey] = imageSource;
                        return imageSource;
                    }
                }
            }
            catch { }
            
            return null;
        }
        
        public static ImageSource? GetIconForExtension(string extension, bool largeIcon = true)
        {
            var cacheKey = $"ext_{extension}_{largeIcon}";
            
            if (_iconCache.TryGetValue(cacheKey, out var cached))
                return cached;
            
            try
            {
                var dummyFile = "dummy" + extension;
                var shfi = new SHFILEINFO();
                var flags = NativeMethods.SHGFI_ICON | NativeMethods.SHGFI_USEFILEATTRIBUTES;
                
                if (largeIcon)
                    flags |= NativeMethods.SHGFI_LARGEICON;
                else
                    flags |= NativeMethods.SHGFI_SMALLICON;
                
                var result = NativeMethods.SHGetFileInfo(dummyFile, 0, ref shfi, 
                    (uint)Marshal.SizeOf(shfi), flags);
                
                if (result != IntPtr.Zero && shfi.hIcon != IntPtr.Zero)
                {
                    var icon = Icon.FromHandle(shfi.hIcon);
                    var imageSource = ConvertIconToImageSource(icon);
                    
                    DestroyIcon(shfi.hIcon);
                    
                    if (imageSource != null)
                    {
                        _iconCache[cacheKey] = imageSource;
                        return imageSource;
                    }
                }
            }
            catch { }
            
            return null;
        }
        
        private static Icon? ExtractIconFromFile(string filePath, bool largeIcon)
        {
            try
            {
                // Try using SHGetFileInfo first
                var shfi = new SHFILEINFO();
                var flags = NativeMethods.SHGFI_ICON;
                
                if (largeIcon)
                    flags |= NativeMethods.SHGFI_LARGEICON;
                else
                    flags |= NativeMethods.SHGFI_SMALLICON;
                
                var result = NativeMethods.SHGetFileInfo(filePath, 0, ref shfi, 
                    (uint)Marshal.SizeOf(shfi), flags);
                
                if (result != IntPtr.Zero && shfi.hIcon != IntPtr.Zero)
                {
                    var icon = Icon.FromHandle(shfi.hIcon);
                    var clonedIcon = (Icon)icon.Clone();
                    DestroyIcon(shfi.hIcon);
                    return clonedIcon;
                }
                
                // Fallback to ExtractIconEx
                IntPtr hIconLarge, hIconSmall;
                int iconCount = NativeMethods.ExtractIconEx(filePath, 0, 
                    out hIconLarge, out hIconSmall, 1);
                
                if (iconCount > 0)
                {
                    var hIcon = largeIcon ? hIconLarge : hIconSmall;
                    if (hIcon != IntPtr.Zero)
                    {
                        var icon = Icon.FromHandle(hIcon);
                        var clonedIcon = (Icon)icon.Clone();
                        DestroyIcon(hIcon);
                        if (hIconLarge != hIcon) DestroyIcon(hIconLarge);
                        if (hIconSmall != hIcon) DestroyIcon(hIconSmall);
                        return clonedIcon;
                    }
                }
            }
            catch { }
            
            return null;
        }
        
        private static ImageSource? ConvertIconToImageSource(Icon icon)
        {
            try
            {
                var bitmap = icon.ToBitmap();
                var hBitmap = bitmap.GetHbitmap();
                
                var imageSource = Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
                
                DeleteObject(hBitmap);
                bitmap.Dispose();
                
                // Freeze for performance
                imageSource.Freeze();
                
                return imageSource;
            }
            catch
            {
                return null;
            }
        }
        
        public static ImageSource? GetSystemIcon(SystemIconType iconType)
        {
            var cacheKey = $"system_{iconType}";
            
            if (_iconCache.TryGetValue(cacheKey, out var cached))
                return cached;
            
            try
            {
                string? path = null;
                
                switch (iconType)
                {
                    case SystemIconType.Folder:
                        path = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                        break;
                    case SystemIconType.Computer:
                        path = "::{20D04FE0-3AEA-1069-A2D8-08002B30309D}";
                        break;
                    case SystemIconType.Network:
                        path = "::{F02C1A0D-BE21-4350-88B0-7367FC96EF3C}";
                        break;
                    case SystemIconType.RecycleBin:
                        path = "::{645FF040-5081-101B-9F08-00AA002F954E}";
                        break;
                    case SystemIconType.Mail:
                        path = @"C:\Program Files\Microsoft Office\root\Office16\OUTLOOK.EXE";
                        if (!System.IO.File.Exists(path))
                            path = @"C:\Windows\System32\shell32.dll"; // Fallback
                        break;
                    case SystemIconType.Settings:
                        path = @"C:\Windows\System32\control.exe";
                        break;
                }
                
                if (path != null)
                {
                    var icon = GetIconForFile(path, true);
                    if (icon != null)
                    {
                        _iconCache[cacheKey] = icon;
                        return icon;
                    }
                }
            }
            catch { }
            
            return null;
        }
        
        public static void ClearCache()
        {
            _iconCache.Clear();
        }
        
        public static void RemoveFromCache(string filePath)
        {
            var keysToRemove = _iconCache.Keys.Where(k => k.StartsWith(filePath)).ToList();
            foreach (var key in keysToRemove)
            {
                _iconCache.Remove(key);
            }
        }
        
        #region P/Invoke
        
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);
        
        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern bool DeleteObject(IntPtr hObject);
        
        #endregion
    }
    
    public enum SystemIconType
    {
        Folder,
        Computer,
        Network,
        RecycleBin,
        Mail,
        Settings
    }
}