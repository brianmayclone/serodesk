using Microsoft.Win32;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SeroDesk.Services
{
    public class WallpaperService
    {
        private static WallpaperService? _instance;
        public static WallpaperService Instance => _instance ?? (_instance = new WallpaperService());
        
        private ImageBrush? _cachedWallpaper;
        private string? _lastWallpaperPath;
        
        public string GetCurrentWallpaperPath()
        {
            return GetWallpaperPath() ?? string.Empty;
        }
        
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