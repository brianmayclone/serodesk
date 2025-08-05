using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using System.Windows;
using Newtonsoft.Json;
using SeroDesk.Models;

namespace SeroDesk.Services
{
    /// <summary>
    /// Centralized configuration manager for all SeroDesk data storage and settings.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class consolidates all configuration management into a single, unified system that manages:
    /// <list type="bullet">
    /// <item>Application settings (UI preferences, behavior, themes)</item>
    /// <item>Launchpad configuration (app layout, groups, positions)</item>
    /// <item>Dock configuration (pinned apps, position, auto-hide)</item>
    /// <item>Widget configuration (positions, types, settings)</item>
    /// <item>System integration settings (shell replacement, startup)</item>
    /// </list>
    /// </para>
    /// <para>
    /// All data is stored in a single JSON file in LocalApplicationData\SeroDesk\config.json
    /// with automatic backup and migration support for configuration upgrades.
    /// </para>
    /// </remarks>
    public class CentralConfigurationManager : INotifyPropertyChanged
    {
        private static CentralConfigurationManager? _instance;
        private SeroConfiguration _configuration;
        private readonly string _configPath;
        private readonly string _backupPath;
        private readonly string _configDirectory;
        
        /// <summary>
        /// Gets the singleton instance of the CentralConfigurationManager.
        /// </summary>
        public static CentralConfigurationManager Instance => _instance ??= new CentralConfigurationManager();

        /// <summary>
        /// Gets the current configuration containing all application settings and data.
        /// </summary>
        public SeroConfiguration Configuration
        {
            get => _configuration;
            private set { _configuration = value; OnPropertyChanged(); }
        }

        private CentralConfigurationManager()
        {
            _configDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SeroDesk");
            
            _configPath = Path.Combine(_configDirectory, "config.json");
            _backupPath = Path.Combine(_configDirectory, "config_backup.json");
            
            _configuration = new SeroConfiguration();
            
            Directory.CreateDirectory(_configDirectory);
        }

        /// <summary>
        /// Loads configuration from disk with automatic migration and error recovery.
        /// </summary>
        public void LoadConfiguration()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    var json = File.ReadAllText(_configPath);
                    var loadedConfig = JsonConvert.DeserializeObject<SeroConfiguration>(json);
                    
                    if (loadedConfig != null)
                    {
                        _configuration = loadedConfig;
                        MigrateConfiguration();
                    }
                }
                else
                {
                    // Try to migrate from old configuration files
                    MigrateFromLegacyFiles();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading configuration: {ex.Message}");
                
                // Try backup file
                if (TryLoadBackup())
                {
                    return;
                }
                
                // Create default configuration
                _configuration = new SeroConfiguration();
                SaveConfiguration();
            }
        }

        /// <summary>
        /// Saves configuration to disk with automatic backup creation.
        /// </summary>
        public void SaveConfiguration()
        {
            try
            {
                // Create backup of current config if it exists
                if (File.Exists(_configPath))
                {
                    File.Copy(_configPath, _backupPath, true);
                }

                var json = JsonConvert.SerializeObject(_configuration, Formatting.Indented);
                File.WriteAllText(_configPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving configuration: {ex.Message}");
            }
        }

        /// <summary>
        /// Migrates from old separate JSON files to the new unified configuration.
        /// </summary>
        private void MigrateFromLegacyFiles()
        {
            try
            {
                // Migrate from old settings.json
                var oldSettingsPath = Path.Combine(_configDirectory, "settings.json");
                if (File.Exists(oldSettingsPath))
                {
                    var json = File.ReadAllText(oldSettingsPath);
                    var oldSettings = JsonConvert.DeserializeObject<dynamic>(json);
                    if (oldSettings != null)
                    {
                        CopyFromLegacySettings(oldSettings);
                    }
                }

                // Migrate from launchpad_config.json
                var launchpadPath = Path.Combine(_configDirectory, "launchpad_config.json");
                if (File.Exists(launchpadPath))
                {
                    var json = File.ReadAllText(launchpadPath);
                    var launchpadConfig = JsonConvert.DeserializeObject<LayoutConfiguration>(json);
                    if (launchpadConfig != null)
                    {
                        _configuration.LaunchpadConfig = launchpadConfig;
                    }
                }

                // Migrate from pinned_apps.json
                var pinnedAppsPath = Path.Combine(_configDirectory, "pinned_apps.json");
                if (File.Exists(pinnedAppsPath))
                {
                    var json = File.ReadAllText(pinnedAppsPath);
                    var pinnedApps = JsonConvert.DeserializeObject<List<string>>(json);
                    if (pinnedApps != null)
                    {
                        _configuration.DockConfig.PinnedApplications = pinnedApps;
                    }
                }

                // Migrate from widgets.json
                var widgetsPath = Path.Combine(_configDirectory, "widgets.json");
                if (File.Exists(widgetsPath))
                {
                    var json = File.ReadAllText(widgetsPath);
                    var widgets = JsonConvert.DeserializeObject<List<Models.SavedWidget>>(json);
                    if (widgets != null)
                    {
                        _configuration.WidgetConfig.SavedWidgets = widgets;
                    }
                }

                SaveConfiguration();
                
                // Clean up old files after successful migration
                CleanupLegacyFiles();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error migrating legacy configuration: {ex.Message}");
            }
        }

        private void CopyFromLegacySettings(dynamic oldSettings)
        {
            // Safely extract values with null checks and defaults
            _configuration.AppSettings.StartWithWindows = oldSettings.StartWithWindows ?? false;
            _configuration.AppSettings.ShowInSystemTray = oldSettings.ShowInSystemTray ?? true;
            _configuration.AppSettings.Theme = oldSettings.Theme ?? "System";
            _configuration.AppSettings.Language = oldSettings.Language ?? "English";
            _configuration.AppSettings.ShellReplacementMode = oldSettings.ShellReplacementMode ?? false;
            
            _configuration.UISettings.EnableAnimations = oldSettings.EnableAnimations ?? true;
            _configuration.UISettings.AnimationSpeed = oldSettings.AnimationSpeed ?? 300;
            _configuration.UISettings.EnableTransparencyEffects = oldSettings.EnableTransparencyEffects ?? true;
            _configuration.UISettings.AnimationSpeedMultiplier = oldSettings.AnimationSpeedMultiplier ?? 1.0;
            _configuration.UISettings.EnableBlurEffect = oldSettings.EnableBlurEffect ?? true;
            _configuration.UISettings.BackgroundOpacity = oldSettings.BackgroundOpacity ?? 0.95;
            
            _configuration.DockConfig.Position = oldSettings.DockPosition ?? "Bottom";
            _configuration.DockConfig.AutoHide = oldSettings.AutoHideTaskbar ?? false;
            _configuration.DockConfig.IconSize = oldSettings.DockIconSize ?? 48;
            _configuration.DockConfig.ShowRecentApps = oldSettings.ShowRecentApps ?? true;
            _configuration.DockConfig.IconScale = oldSettings.IconScale ?? 1.0;
            _configuration.DockConfig.IconSpacing = oldSettings.IconSpacing ?? 20;
            
            _configuration.SystemSettings.EnableWidgets = oldSettings.EnableWidgets ?? true;
            _configuration.SystemSettings.EnableTouchGestures = oldSettings.EnableTouchGestures ?? true;
            _configuration.SystemSettings.TouchSensitivity = oldSettings.TouchSensitivity ?? 1.0;
        }

        private void CleanupLegacyFiles()
        {
            try
            {
                var legacyFiles = new[]
                {
                    "settings.json",
                    "launchpad_config.json", 
                    "pinned_apps.json",
                    "widgets.json",
                    "desktop_layout.json",
                    "layout_config.json"
                };

                foreach (var file in legacyFiles)
                {
                    var path = Path.Combine(_configDirectory, file);
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                        System.Diagnostics.Debug.WriteLine($"Cleaned up legacy file: {file}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cleaning up legacy files: {ex.Message}");
            }
        }

        private bool TryLoadBackup()
        {
            try
            {
                if (File.Exists(_backupPath))
                {
                    var json = File.ReadAllText(_backupPath);
                    var backupConfig = JsonConvert.DeserializeObject<SeroConfiguration>(json);
                    
                    if (backupConfig != null)
                    {
                        _configuration = backupConfig;
                        SaveConfiguration(); // Restore from backup
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading backup configuration: {ex.Message}");
            }
            
            return false;
        }

        private void MigrateConfiguration()
        {
            // Future configuration version migrations will go here
            if (_configuration.ConfigVersion < SeroConfiguration.CurrentVersion)
            {
                System.Diagnostics.Debug.WriteLine($"Migrating configuration from version {_configuration.ConfigVersion} to {SeroConfiguration.CurrentVersion}");
                
                _configuration.ConfigVersion = SeroConfiguration.CurrentVersion;
                SaveConfiguration();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            
            // Auto-save on any configuration change
            SaveConfiguration();
        }
    }

    /// <summary>
    /// Unified configuration container for all SeroDesk settings and data.
    /// </summary>
    public class SeroConfiguration : INotifyPropertyChanged
    {
        public const int CurrentVersion = 1;
        
        public int ConfigVersion { get; set; } = CurrentVersion;
        public DateTime LastModified { get; set; } = DateTime.Now;
        
        public ApplicationSettings AppSettings { get; set; } = new();
        public UISettings UISettings { get; set; } = new();
        public DockConfiguration DockConfig { get; set; } = new();
        public LayoutConfiguration LaunchpadConfig { get; set; } = new();
        public WidgetConfiguration WidgetConfig { get; set; } = new();
        public SystemSettings SystemSettings { get; set; } = new();

        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            LastModified = DateTime.Now;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Application-level settings (startup, language, theme).
    /// </summary>
    public class ApplicationSettings : INotifyPropertyChanged
    {
        private bool _startWithWindows = false;
        private bool _showInSystemTray = true;
        private string _theme = "System";
        private string _language = "English";
        private bool _shellReplacementMode = false;

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

        public string Theme
        {
            get => _theme;
            set { _theme = value; OnPropertyChanged(); }
        }

        public string Language
        {
            get => _language;
            set { _language = value; OnPropertyChanged(); }
        }

        public bool ShellReplacementMode
        {
            get => _shellReplacementMode;
            set { _shellReplacementMode = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// UI appearance and animation settings.
    /// </summary>
    public class UISettings : INotifyPropertyChanged
    {
        private bool _enableAnimations = true;
        private int _animationSpeed = 300;
        private bool _enableTransparencyEffects = true;
        private double _animationSpeedMultiplier = 1.0;
        private bool _enableBlurEffect = true;
        private double _backgroundOpacity = 0.95;

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

    /// <summary>
    /// Dock appearance, behavior and pinned applications.
    /// </summary>
    public class DockConfiguration : INotifyPropertyChanged
    {
        private string _position = "Bottom";
        private bool _autoHide = false;
        private int _iconSize = 48;
        private bool _showRecentApps = true;
        private double _iconScale = 1.0;
        private int _iconSpacing = 20;
        private List<string> _pinnedApplications = new();

        public string Position
        {
            get => _position;
            set { _position = value; OnPropertyChanged(); }
        }

        public bool AutoHide
        {
            get => _autoHide;
            set { _autoHide = value; OnPropertyChanged(); }
        }

        public int IconSize
        {
            get => _iconSize;
            set { _iconSize = Math.Max(32, Math.Min(64, value)); OnPropertyChanged(); }
        }

        public bool ShowRecentApps
        {
            get => _showRecentApps;
            set { _showRecentApps = value; OnPropertyChanged(); }
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

        public List<string> PinnedApplications
        {
            get => _pinnedApplications;
            set { _pinnedApplications = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Widget configuration and saved widget data.
    /// </summary>
    public class WidgetConfiguration : INotifyPropertyChanged
    {
        private bool _enableWidgets = true;
        private List<Models.SavedWidget> _savedWidgets = new();

        public bool EnableWidgets
        {
            get => _enableWidgets;
            set { _enableWidgets = value; OnPropertyChanged(); }
        }

        public List<Models.SavedWidget> SavedWidgets
        {
            get => _savedWidgets;
            set { _savedWidgets = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// System integration and touch gesture settings.
    /// </summary>
    public class SystemSettings : INotifyPropertyChanged
    {
        private bool _enableWidgets = true;
        private bool _enableTouchGestures = true;
        private double _touchSensitivity = 1.0;

        public bool EnableWidgets
        {
            get => _enableWidgets;
            set { _enableWidgets = value; OnPropertyChanged(); }
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

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}