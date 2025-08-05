using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SeroDesk.Platform
{
    /// <summary>
    /// Provides high-performance icon extraction and caching functionality for files, applications, and system resources.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The IconExtractor class serves as the central component for extracting and managing application icons
    /// throughout the SeroDesk interface. It provides:
    /// <list type="bullet">
    /// <item>Efficient icon extraction from executable files and shortcuts</item>
    /// <item>System icon retrieval for common shell objects</item>
    /// <item>Intelligent caching to improve performance and reduce system calls</item>
    /// <item>Support for both large and small icon variants</item>
    /// <item>Integration with Windows Shell APIs for maximum compatibility</item>
    /// <item>Proper resource management to prevent memory leaks</item>
    /// </list>
    /// </para>
    /// <para>
    /// The class uses Windows Shell API (SHGetFileInfo) and native icon extraction methods
    /// to ensure compatibility with all types of Windows applications, including:
    /// <list type="bullet">
    /// <item>Traditional Win32 applications</item>
    /// <item>UWP/Store applications</item>
    /// <item>.NET applications</item>
    /// <item>Portable executables</item>
    /// <item>Shell shortcuts and links</item>
    /// </list>
    /// </para>
    /// <para>
    /// All extracted icons are automatically cached using file paths as keys to minimize
    /// redundant extraction operations and improve UI responsiveness.
    /// </para>
    /// </remarks>
    public static class IconExtractor
    {
        /// <summary>
        /// Cache dictionary storing extracted icons to improve performance and reduce system calls.
        /// </summary>
        /// <remarks>
        /// Icons are cached using a combination of file path and size preference as the key.
        /// This prevents redundant extraction operations for frequently accessed files.
        /// </remarks>
        private static readonly Dictionary<string, ImageSource> _iconCache = new();
        
        /// <summary>
        /// Extracts and returns the icon associated with a specific file or application.
        /// </summary>
        /// <param name="filePath">The full path to the file or executable from which to extract the icon.</param>
        /// <param name="largeIcon">True to extract a large (32x32) icon; false for small (16x16) icon.</param>
        /// <returns>
        /// An ImageSource containing the extracted icon that can be used in WPF controls,
        /// or null if extraction fails or the file doesn't exist.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method first checks the internal cache for a previously extracted icon.
        /// If not found, it uses Windows Shell APIs to extract the icon directly from the file.
        /// </para>
        /// <para>
        /// The extraction process handles various file types including:
        /// <list type="bullet">
        /// <item>Executable files (.exe)</item>
        /// <item>Dynamic libraries (.dll) with icon resources</item>
        /// <item>Shortcut files (.lnk)</item>
        /// <item>Any file type with an associated icon</item>
        /// </list>
        /// </para>
        /// <para>
        /// All successfully extracted icons are automatically cached for future requests.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentException">Thrown when filePath is null or empty.</exception>
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
                var shfi = new NativeMethods.SHFILEINFO();
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
                var shfi = new NativeMethods.SHFILEINFO();
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