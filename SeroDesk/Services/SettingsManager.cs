using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace SeroDesk.Services
{
    /// <summary>
    /// Manages application settings and configuration persistence for SeroDesk.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The SettingsManager provides centralized configuration management using a singleton pattern.
    /// It handles:
    /// <list type="bullet">
    /// <item>Loading and saving user preferences from JSON files</item>
    /// <item>Providing default values for first-time users</item>
    /// <item>Automatic settings persistence on application shutdown</item>
    /// <item>Property change notifications for UI binding</item>
    /// <item>Error handling for corrupted or missing configuration files</item>
    /// </list>
    /// </para>
    /// <para>
    /// Settings are stored in the user's LocalApplicationData folder under SeroDesk\settings.json
    /// to ensure they persist across application updates and user sessions.
    /// </para>
    /// <para>
    /// The class implements INotifyPropertyChanged to support data binding scenarios where
    /// UI elements need to reflect real-time changes to settings.
    /// </para>
    /// </remarks>
    public class SettingsManager : INotifyPropertyChanged
    {
        /// <summary>
        /// Singleton instance of the SettingsManager.
        /// </summary>
        private static SettingsManager? _instance;
        
        /// <summary>
        /// Current settings instance containing all configuration values.
        /// </summary>
        private Settings _settings;
        
        /// <summary>
        /// Full path to the settings JSON file on disk.
        /// </summary>
        private readonly string _settingsPath;
        
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
        /// Setting this property triggers property change notifications for data binding scenarios.
        /// </remarks>
        public Settings Settings
        {
            get => _settings;
            set { _settings = value; OnPropertyChanged(); }
        }
        
        private SettingsManager()
        {
            _settingsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SeroDesk", "settings.json");
            
            _settings = new Settings();
        }
        
        /// <summary>
        /// Loads settings from the persistent storage file.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method attempts to load settings from the JSON file in LocalApplicationData.
        /// If the file doesn't exist or contains invalid JSON, default settings are used instead.
        /// </para>
        /// <para>
        /// The method is designed to be fault-tolerant, ensuring the application can start
        /// even if the settings file is corrupted or missing.
        /// </para>
        /// </remarks>
        public void LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    var json = File.ReadAllText(_settingsPath);
                    _settings = JsonConvert.DeserializeObject<Settings>(json) ?? new Settings();
                }
            }
            catch
            {
                _settings = new Settings();
            }
        }
        
        public void SaveSettings()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
                var json = JsonConvert.SerializeObject(_settings, Formatting.Indented);
                File.WriteAllText(_settingsPath, json);
            }
            catch { }
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            SaveSettings();
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
        private string _theme = "Dark";
        private bool _autoHideTaskbar = false;
        private int _taskbarHeight = 48;
        private bool _enableWidgets = true;
        private bool _enableBlurEffect = true;
        private double _backgroundOpacity = 0.95;
        
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
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}