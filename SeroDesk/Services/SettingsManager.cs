using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace SeroDesk.Services
{
    public class SettingsManager : INotifyPropertyChanged
    {
        private static SettingsManager? _instance;
        private Settings _settings;
        private readonly string _settingsPath;
        
        public static SettingsManager Instance => _instance ?? (_instance = new SettingsManager());
        
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