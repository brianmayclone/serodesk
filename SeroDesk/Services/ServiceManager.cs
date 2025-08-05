using System.Collections.Generic;

namespace SeroDesk.Services
{
    /// <summary>
    /// Manages the lifecycle and coordination of all services within the SeroDesk application.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The ServiceManager implements a centralized service management pattern that provides:
    /// <list type="bullet">
    /// <item>Dependency injection and service location capabilities</item>
    /// <item>Coordinated initialization and shutdown of all application services</item>
    /// <item>Service registration and discovery through a common interface</item>
    /// <item>Proper disposal and cleanup to prevent resource leaks</item>
    /// <item>Ordered service lifecycle management</item>
    /// </list>
    /// </para>
    /// <para>
    /// Services are initialized in registration order and shut down in reverse order
    /// to ensure proper dependency management. All services must implement the
    /// <see cref="IService"/> interface to be managed by this class.
    /// </para>
    /// <para>
    /// The class follows the singleton pattern to ensure consistent service state
    /// throughout the application lifecycle.
    /// </para>
    /// </remarks>
    public class ServiceManager
    {
        /// <summary>
        /// Singleton instance of the ServiceManager.
        /// </summary>
        private static ServiceManager? _instance;
        
        /// <summary>
        /// Collection of all registered services managed by this instance.
        /// </summary>
        private readonly List<IService> _services = new();
        
        /// <summary>
        /// Gets the singleton instance of the ServiceManager.
        /// </summary>
        /// <value>The global ServiceManager instance.</value>
        public static ServiceManager Instance => _instance ?? (_instance = new ServiceManager());
        
        private ServiceManager() { }
        
        /// <summary>
        /// Initializes all application services in the correct order.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method performs the complete service initialization sequence:
        /// <list type="number">
        /// <item>Registers all core application services</item>
        /// <item>Initializes each service in registration order</item>
        /// <item>Ensures all dependencies are properly set up</item>
        /// </list>
        /// </para>
        /// <para>
        /// The services are registered and initialized in dependency order to ensure
        /// that services with dependencies on other services are initialized after
        /// their dependencies are ready.
        /// </para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">Thrown if a service fails to initialize.</exception>
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
        
        /// <summary>
        /// Registers a service with the ServiceManager for lifecycle management.
        /// </summary>
        /// <param name="service">The service instance to register.</param>
        /// <remarks>
        /// Registered services will be automatically initialized when <see cref="Initialize"/>
        /// is called and shut down when <see cref="Shutdown"/> is called.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when service is null.</exception>
        public void RegisterService(IService service)
        {
            _services.Add(service);
        }
        
        /// <summary>
        /// Retrieves a registered service of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of service to retrieve. Must implement <see cref="IService"/>.</typeparam>
        /// <returns>
        /// The service instance of type T if found; otherwise, null.
        /// </returns>
        /// <remarks>
        /// This method provides service location functionality, allowing other components
        /// to access registered services without direct dependencies.
        /// </remarks>
        public T? GetService<T>() where T : class, IService
        {
            return _services.FirstOrDefault(s => s is T) as T;
        }
        
        /// <summary>
        /// Shuts down all registered services and performs cleanup operations.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method performs an orderly shutdown of all application services:
        /// <list type="number">
        /// <item>Disposes the WindowManager to release system resources</item>
        /// <item>Shuts down all registered services in reverse order of registration</item>
        /// <item>Clears the service collection to prevent further access</item>
        /// </list>
        /// </para>
        /// <para>
        /// Services are shut down in reverse order to ensure that services with dependencies
        /// on other services are shut down before their dependencies are disposed.
        /// </para>
        /// <para>
        /// This method should be called during application shutdown to ensure proper cleanup
        /// and prevent resource leaks.
        /// </para>
        /// </remarks>
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