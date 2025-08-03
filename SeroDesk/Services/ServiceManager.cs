using System.Collections.Generic;

namespace SeroDesk.Services
{
    public class ServiceManager
    {
        private static ServiceManager? _instance;
        private readonly List<IService> _services = new();
        
        public static ServiceManager Instance => _instance ?? (_instance = new ServiceManager());
        
        private ServiceManager() { }
        
        public void Initialize()
        {
            // Register all services
            RegisterService(new IconCacheService());
            RegisterService(new ConfigurationService());
            RegisterService(new ThemeService());
            
            // Initialize all services
            foreach (var service in _services)
            {
                service.Initialize();
            }
        }
        
        public void RegisterService(IService service)
        {
            _services.Add(service);
        }
        
        public T? GetService<T>() where T : class, IService
        {
            return _services.FirstOrDefault(s => s is T) as T;
        }
        
        public void Shutdown()
        {
            // Dispose WindowManager
            Platform.WindowManager.Instance.Dispose();
            
            // Shutdown in reverse order
            for (int i = _services.Count - 1; i >= 0; i--)
            {
                _services[i].Shutdown();
            }
            _services.Clear();
        }
    }
    
    public interface IService
    {
        void Initialize();
        void Shutdown();
    }
    
    public class IconCacheService : IService
    {
        private readonly Dictionary<string, System.Windows.Media.ImageSource> _cache = new();
        private readonly object _lock = new();
        
        public void Initialize()
        {
            // Setup cache cleanup timer
        }
        
        public void Shutdown()
        {
            lock (_lock)
            {
                _cache.Clear();
            }
        }
        
        public System.Windows.Media.ImageSource? GetCachedIcon(string key)
        {
            lock (_lock)
            {
                return _cache.TryGetValue(key, out var icon) ? icon : null;
            }
        }
        
        public void CacheIcon(string key, System.Windows.Media.ImageSource icon)
        {
            lock (_lock)
            {
                _cache[key] = icon;
            }
        }
    }
    
    public class ConfigurationService : IService
    {
        private Dictionary<string, object> _settings = new();
        
        public void Initialize()
        {
            LoadConfiguration();
        }
        
        public void Shutdown()
        {
            SaveConfiguration();
        }
        
        public T? GetSetting<T>(string key, T? defaultValue = default)
        {
            return _settings.TryGetValue(key, out var value) ? (T)value : defaultValue;
        }
        
        public void SetSetting<T>(string key, T value)
        {
            _settings[key] = value!;
        }
        
        private void LoadConfiguration()
        {
            // Load from file
        }
        
        private void SaveConfiguration()
        {
            // Save to file
        }
    }
    
    public class ThemeService : IService
    {
        public enum Theme
        {
            Light,
            Dark,
            System
        }
        
        private Theme _currentTheme = Theme.Dark;
        
        public Theme CurrentTheme => _currentTheme;
        
        public void Initialize()
        {
            ApplyTheme(_currentTheme);
        }
        
        public void Shutdown()
        {
        }
        
        public void SetTheme(Theme theme)
        {
            _currentTheme = theme;
            ApplyTheme(theme);
        }
        
        private void ApplyTheme(Theme theme)
        {
            // Apply theme to application resources
        }
    }
}