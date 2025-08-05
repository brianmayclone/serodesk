using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SeroDesk.Services
{
    /// <summary>
    /// Legacy SettingsManager that now acts as a wrapper around CentralConfigurationManager.
    /// This class maintains backward compatibility while delegating to the new unified configuration system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The SettingsManager now serves as a compatibility layer that:
    /// <list type="bullet">
    /// <item>Provides the same API as before for existing code</item>
    /// <item>Delegates all operations to CentralConfigurationManager</item>
    /// <item>Converts between legacy Settings objects and new configuration structure</item>
    /// <item>Maintains property change notifications for UI binding</item>
    /// <item>Ensures seamless migration from old to new configuration system</item>
    /// </list>
    /// </para>
    /// <para>
    /// All configuration data is now stored in the unified config.json file managed by
    /// CentralConfigurationManager, with automatic migration from legacy files.
    /// </para>
    /// </remarks>
    public class SettingsManager : INotifyPropertyChanged
    {
        /// <summary>
        /// Singleton instance of the SettingsManager.
        /// </summary>
        private static SettingsManager? _instance;
        
        /// <summary>
        /// Reference to the centralized configuration manager.
        /// </summary>
        private readonly CentralConfigurationManager _centralConfig;
        
        /// <summary>
        /// Gets the singleton instance of the SettingsManager.
        /// </summary>
        /// <value>The global SettingsManager instance.</value>
        /// <remarks>
        /// The instance is created on first access and maintained throughout the application lifetime.
        /// </remarks>
        public static SettingsManager Instance => _instance ?? (_instance = new SettingsManager());
        
        /// <summary>
        /// Gets or sets the current settings object containing all configuration values.
        /// </summary>
        /// <value>A Settings instance with all current configuration values.</value>
        /// <remarks>
        /// This property now converts between the legacy Settings format and the new unified configuration.
        /// Setting this property triggers property change notifications for data binding scenarios.
        /// </remarks>
        public Settings Settings
        {
            get => ConvertFromCentralConfig();
            set { ConvertToCentralConfig(value); OnPropertyChanged(); }
        }
        
        private SettingsManager()
        {
            _centralConfig = CentralConfigurationManager.Instance;
            _centralConfig.LoadConfiguration();
            
            // Subscribe to central config changes to forward property change notifications
            _centralConfig.PropertyChanged += (s, e) => OnPropertyChanged(nameof(Settings));
        }
        
        /// <summary>
        /// Loads settings from the persistent storage file.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method now delegates to CentralConfigurationManager to load the unified configuration
        /// and converts it to the legacy Settings format for backward compatibility.
        /// </para>
        /// <para>
        /// The method handles automatic migration from old settings files to the new unified format.
        /// </para>
        /// </remarks>
        public Settings LoadSettings()
        {
            _centralConfig.LoadConfiguration();
            return ConvertFromCentralConfig();
        }
        
        /// <summary>
        /// Saves settings to persistent storage.
        /// </summary>
        /// <remarks>
        /// This method now delegates to CentralConfigurationManager to save the unified configuration.
        /// </remarks>
        public void SaveSettings()
        {
            _centralConfig.SaveConfiguration();
        }
        
        /// <summary>
        /// Saves the provided settings to persistent storage.
        /// </summary>
        /// <param name="settings">The settings object to save.</param>
        /// <remarks>
        /// This method converts the legacy Settings object to the new unified configuration format
        /// and saves it through CentralConfigurationManager.
        /// </remarks>
        public void SaveSettings(Settings settings)
        {
            ConvertToCentralConfig(settings);
            _centralConfig.SaveConfiguration();
        }
        
        /// <summary>
        /// Converts the unified configuration to legacy Settings format.
        /// </summary>
        /// <returns>A Settings object populated from the central configuration.</returns>
        private Settings ConvertFromCentralConfig()
        {
            var config = _centralConfig.Configuration;
            return new Settings
            {
                StartWithWindows = config.AppSettings.StartWithWindows,
                ShowInSystemTray = config.AppSettings.ShowInSystemTray,
                Theme = config.AppSettings.Theme,
                Language = config.AppSettings.Language,
                ShellReplacementMode = config.AppSettings.ShellReplacementMode,
                
                EnableAnimations = config.UISettings.EnableAnimations,
                AnimationSpeed = config.UISettings.AnimationSpeed,
                EnableTransparencyEffects = config.UISettings.EnableTransparencyEffects,
                AnimationSpeedMultiplier = config.UISettings.AnimationSpeedMultiplier,
                EnableBlurEffect = config.UISettings.EnableBlurEffect,
                BackgroundOpacity = config.UISettings.BackgroundOpacity,
                
                DockPosition = config.DockConfig.Position,
                AutoHideTaskbar = config.DockConfig.AutoHide,
                DockIconSize = config.DockConfig.IconSize,
                ShowRecentApps = config.DockConfig.ShowRecentApps,
                IconScale = config.DockConfig.IconScale,
                IconSpacing = config.DockConfig.IconSpacing,
                
                EnableWidgets = config.SystemSettings.EnableWidgets,
                EnableTouchGestures = config.SystemSettings.EnableTouchGestures,
                TouchSensitivity = config.SystemSettings.TouchSensitivity,
                
                // Legacy properties for backward compatibility
                TaskbarHeight = config.DockConfig.IconSize // Map dock icon size to taskbar height
            };
        }
        
        /// <summary>
        /// Converts legacy Settings format to the unified configuration.
        /// </summary>
        /// <param name="settings">The legacy Settings object to convert.</param>
        private void ConvertToCentralConfig(Settings settings)
        {
            var config = _centralConfig.Configuration;
            
            config.AppSettings.StartWithWindows = settings.StartWithWindows;
            config.AppSettings.ShowInSystemTray = settings.ShowInSystemTray;
            config.AppSettings.Theme = settings.Theme;
            config.AppSettings.Language = settings.Language;
            config.AppSettings.ShellReplacementMode = settings.ShellReplacementMode;
            
            config.UISettings.EnableAnimations = settings.EnableAnimations;
            config.UISettings.AnimationSpeed = settings.AnimationSpeed;
            config.UISettings.EnableTransparencyEffects = settings.EnableTransparencyEffects;
            config.UISettings.AnimationSpeedMultiplier = settings.AnimationSpeedMultiplier;
            config.UISettings.EnableBlurEffect = settings.EnableBlurEffect;
            config.UISettings.BackgroundOpacity = settings.BackgroundOpacity;
            
            config.DockConfig.Position = settings.DockPosition;
            config.DockConfig.AutoHide = settings.AutoHideTaskbar;
            config.DockConfig.IconSize = settings.DockIconSize;
            config.DockConfig.ShowRecentApps = settings.ShowRecentApps;
            config.DockConfig.IconScale = settings.IconScale;
            config.DockConfig.IconSpacing = settings.IconSpacing;
            
            config.SystemSettings.EnableWidgets = settings.EnableWidgets;
            config.SystemSettings.EnableTouchGestures = settings.EnableTouchGestures;
            config.SystemSettings.TouchSensitivity = settings.TouchSensitivity;
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    
    public class Settings : INotifyPropertyChanged
    {
        private bool _startWithWindows = false;
        private bool _showInSystemTray = true;
        private double _iconScale = 1.0;
        private int _iconSpacing = 20;
        private bool _enableAnimations = true;
        private int _animationSpeed = 300;
        private bool _enableTouchGestures = true;
        private double _touchSensitivity = 1.0;
        private string _theme = "System";
        private bool _autoHideTaskbar = false;
        private int _taskbarHeight = 48;
        private bool _enableWidgets = true;
        private bool _enableBlurEffect = true;
        private double _backgroundOpacity = 0.95;
        private bool _shellReplacementMode = false;
        private string _language = "English";
        private string _dockPosition = "Bottom";
        private bool _showRecentApps = true;
        private int _dockIconSize = 48;
        private bool _enableTransparencyEffects = true;
        private double _animationSpeedMultiplier = 1.0;
        
        public bool StartWithWindows
        {
            get => _startWithWindows;
            set { _startWithWindows = value; OnPropertyChanged(); }
        }
        
        public bool ShowInSystemTray
        {
            get => _showInSystemTray;
            set { _showInSystemTray = value; OnPropertyChanged(); }
        }
        
        public double IconScale
        {
            get => _iconScale;
            set { _iconScale = Math.Max(0.5, Math.Min(2.0, value)); OnPropertyChanged(); }
        }
        
        public int IconSpacing
        {
            get => _iconSpacing;
            set { _iconSpacing = Math.Max(10, Math.Min(50, value)); OnPropertyChanged(); }
        }
        
        public bool EnableAnimations
        {
            get => _enableAnimations;
            set { _enableAnimations = value; OnPropertyChanged(); }
        }
        
        public int AnimationSpeed
        {
            get => _animationSpeed;
            set { _animationSpeed = Math.Max(100, Math.Min(1000, value)); OnPropertyChanged(); }
        }
        
        public bool EnableTouchGestures
        {
            get => _enableTouchGestures;
            set { _enableTouchGestures = value; OnPropertyChanged(); }
        }
        
        public double TouchSensitivity
        {
            get => _touchSensitivity;
            set { _touchSensitivity = Math.Max(0.1, Math.Min(2.0, value)); OnPropertyChanged(); }
        }
        
        public string Theme
        {
            get => _theme;
            set { _theme = value; OnPropertyChanged(); }
        }
        
        public bool AutoHideTaskbar
        {
            get => _autoHideTaskbar;
            set { _autoHideTaskbar = value; OnPropertyChanged(); }
        }
        
        public int TaskbarHeight
        {
            get => _taskbarHeight;
            set { _taskbarHeight = Math.Max(32, Math.Min(64, value)); OnPropertyChanged(); }
        }
        
        public bool EnableWidgets
        {
            get => _enableWidgets;
            set { _enableWidgets = value; OnPropertyChanged(); }
        }
        
        public bool EnableBlurEffect
        {
            get => _enableBlurEffect;
            set { _enableBlurEffect = value; OnPropertyChanged(); }
        }
        
        public double BackgroundOpacity
        {
            get => _backgroundOpacity;
            set { _backgroundOpacity = Math.Max(0.0, Math.Min(1.0, value)); OnPropertyChanged(); }
        }
        
        public bool ShellReplacementMode
        {
            get => _shellReplacementMode;
            set { _shellReplacementMode = value; OnPropertyChanged(); }
        }
        
        public string Language
        {
            get => _language;
            set { _language = value; OnPropertyChanged(); }
        }
        
        public string DockPosition
        {
            get => _dockPosition;
            set { _dockPosition = value; OnPropertyChanged(); }
        }
        
        public bool ShowRecentApps
        {
            get => _showRecentApps;
            set { _showRecentApps = value; OnPropertyChanged(); }
        }
        
        public int DockIconSize
        {
            get => _dockIconSize;
            set { _dockIconSize = Math.Max(32, Math.Min(64, value)); OnPropertyChanged(); }
        }
        
        public bool EnableTransparencyEffects
        {
            get => _enableTransparencyEffects;
            set { _enableTransparencyEffects = value; OnPropertyChanged(); }
        }
        
        public double AnimationSpeedMultiplier
        {
            get => _animationSpeedMultiplier;
            set { _animationSpeedMultiplier = Math.Max(0.5, Math.Min(2.0, value)); OnPropertyChanged(); }
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}