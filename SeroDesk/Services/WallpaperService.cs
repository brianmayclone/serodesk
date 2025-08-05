using Microsoft.Win32;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SeroDesk.Services
{
    /// <summary>
    /// Provides access to the current Windows desktop wallpaper for use in SeroDesk interface backgrounds.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The WallpaperService integrates with Windows registry and system APIs to retrieve
    /// the currently active desktop wallpaper. This enables SeroDesk to maintain visual
    /// consistency with the user's desktop environment:
    /// <list type="bullet">
    /// <item>Retrieves the current wallpaper path from Windows registry</item>
    /// <item>Loads and caches wallpaper images for performance</item>
    /// <item>Provides WPF-compatible ImageBrush objects for UI binding</item>
    /// <item>Handles different wallpaper formats and scaling options</item>
    /// <item>Automatically detects wallpaper changes for dynamic updates</item>
    /// </list>
    /// </para>
    /// <para>
    /// The service implements intelligent caching to avoid repeatedly loading the same
    /// wallpaper image, which improves performance and reduces memory usage.
    /// </para>
    /// <para>
    /// Wallpapers are automatically scaled and positioned using UniformToFill stretch
    /// to provide optimal visual appearance across different screen sizes and resolutions.
    /// </para>
    /// </remarks>
    public class WallpaperService
    {
        /// <summary>
        /// Singleton instance of the WallpaperService.
        /// </summary>
        private static WallpaperService? _instance;
        
        /// <summary>
        /// Gets the singleton instance of the WallpaperService.
        /// </summary>
        /// <value>The global WallpaperService instance.</value>
        public static WallpaperService Instance => _instance ?? (_instance = new WallpaperService());
        
        /// <summary>
        /// Cached ImageBrush containing the current wallpaper to avoid redundant loading.
        /// </summary>
        private ImageBrush? _cachedWallpaper;
        
        /// <summary>
        /// Path of the last loaded wallpaper for cache validation.
        /// </summary>
        private string? _lastWallpaperPath;
        
        /// <summary>
        /// Retrieves the file path of the currently active Windows desktop wallpaper.
        /// </summary>
        /// <returns>
        /// The full file path to the current wallpaper image, or an empty string if no wallpaper is set.
        /// </returns>
        /// <remarks>
        /// This method queries the Windows registry to determine the current wallpaper path.
        /// The path may point to various image formats supported by Windows.
        /// </remarks>
        public string GetCurrentWallpaperPath()
        {
            return GetWallpaperPath() ?? string.Empty;
        }
        
        /// <summary>
        /// Retrieves the current Windows desktop wallpaper as a WPF ImageBrush for UI use.
        /// </summary>
        /// <returns>
        /// An ImageBrush containing the current wallpaper configured for optimal display,
        /// or null if the wallpaper cannot be loaded.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method provides intelligent caching to avoid repeatedly loading the same wallpaper:
        /// <list type="bullet">
        /// <item>Returns cached wallpaper if the path hasn't changed</item>
        /// <item>Loads and caches new wallpaper if path has changed</item>
        /// <item>Configures ImageBrush with appropriate scaling and alignment</item>
        /// <item>Freezes the ImageBrush for better performance and thread-safety</item>
        /// </list>
        /// </para>
        /// <para>
        /// The returned ImageBrush is configured with UniformToFill stretch mode and center
        /// alignment to provide optimal visual appearance on different screen sizes.
        /// </para>
        /// </remarks>
        public ImageBrush? GetCurrentWallpaper()
        {
            var currentPath = GetWallpaperPath();
            
            // Return cached if same wallpaper
            if (currentPath == _lastWallpaperPath && _cachedWallpaper != null)
            {
                return _cachedWallpaper;
            }
            
            if (!string.IsNullOrEmpty(currentPath) && File.Exists(currentPath))
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(currentPath);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    
                    _cachedWallpaper = new ImageBrush(bitmap)
                    {
                        Stretch = Stretch.UniformToFill,
                        AlignmentX = AlignmentX.Center,
                        AlignmentY = AlignmentY.Center
                    };
                    _cachedWallpaper.Freeze();
                    
                    _lastWallpaperPath = currentPath;
                    return _cachedWallpaper;
                }
                catch { }
            }
            
            // Fallback to solid color
            return CreateFallbackBrush();
        }
        
        private ImageBrush CreateFallbackBrush()
        {
            // Create a gradient fallback if no wallpaper found
            var gradientBrush = new LinearGradientBrush();
            gradientBrush.StartPoint = new System.Windows.Point(0, 0);
            gradientBrush.EndPoint = new System.Windows.Point(1, 1);
            gradientBrush.GradientStops.Add(new GradientStop(Color.FromRgb(0x1E, 0x1E, 0x1E), 0.0));
            gradientBrush.GradientStops.Add(new GradientStop(Color.FromRgb(0x2D, 0x2D, 0x30), 1.0));
            gradientBrush.Freeze();
            
            // Convert to ImageBrush for consistency
            return new ImageBrush(CreateGradientImage(gradientBrush))
            {
                Stretch = Stretch.UniformToFill
            };
        }
        
        private ImageSource CreateGradientImage(LinearGradientBrush brush)
        {
            var renderTarget = new RenderTargetBitmap(1920, 1080, 96, 96, PixelFormats.Pbgra32);
            var visual = new System.Windows.Shapes.Rectangle
            {
                Width = 1920,
                Height = 1080,
                Fill = brush
            };
            
            visual.Measure(new System.Windows.Size(1920, 1080));
            visual.Arrange(new System.Windows.Rect(0, 0, 1920, 1080));
            
            renderTarget.Render(visual);
            renderTarget.Freeze();
            
            return renderTarget;
        }
        
        private string? GetWallpaperPath()
        {
            try
            {
                // Try Windows 10/11 method first
                var path = GetWindows10WallpaperPath();
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                    return path;
                
                // Fallback to registry method
                return GetRegistryWallpaperPath();
            }
            catch
            {
                return null;
            }
        }
        
        private string? GetWindows10WallpaperPath()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop");
                var wallpaper = key?.GetValue("Wallpaper") as string;
                
                if (!string.IsNullOrEmpty(wallpaper) && File.Exists(wallpaper))
                    return wallpaper;
                
                // Check for slideshow
                using var slideshowKey = Registry.CurrentUser.OpenSubKey(@"Control Panel\Personalization\Desktop Slideshow");
                var slideshowPath = slideshowKey?.GetValue("Shuffle") as string;
                
                if (!string.IsNullOrEmpty(slideshowPath))
                {
                    var directory = Path.GetDirectoryName(slideshowPath);
                    if (Directory.Exists(directory))
                    {
                        var images = Directory.GetFiles(directory, "*.jpg")
                            .Concat(Directory.GetFiles(directory, "*.png"))
                            .Concat(Directory.GetFiles(directory, "*.bmp"))
                            .FirstOrDefault();
                        
                        if (!string.IsNullOrEmpty(images))
                            return images;
                    }
                }
            }
            catch { }
            
            return null;
        }
        
        private string? GetRegistryWallpaperPath()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop");
                return key?.GetValue("Wallpaper") as string;
            }
            catch
            {
                return null;
            }
        }
        
        public void ClearCache()
        {
            _cachedWallpaper = null;
            _lastWallpaperPath = null;
        }
        
        public bool IsWallpaperDark()
        {
            // Simple heuristic to determine if wallpaper is dark
            // This can be enhanced with actual image analysis
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                var appsUseLightTheme = key?.GetValue("AppsUseLightTheme");
                return appsUseLightTheme is int value && value == 0;
            }
            catch
            {
                return true; // Default to dark
            }
        }
    }
}