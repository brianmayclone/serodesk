using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace SeroDesk.Services
{
    /// <summary>
    /// Manages application theming including dark/light mode switching and system theme detection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The ThemeManager provides centralized theme management that:
    /// <list type="bullet">
    /// <item>Detects Windows system theme preferences (Light/Dark)</item>
    /// <item>Applies themes dynamically to all application windows</item>
    /// <item>Manages transparency and glassmorphism effects</item>
    /// <item>Handles theme change notifications from Windows</item>
    /// <item>Coordinates with CentralConfigurationManager for persistence</item>
    /// </list>
    /// </para>
    /// <para>
    /// Themes are applied through WPF ResourceDictionaries and can be switched
    /// at runtime without requiring application restart.
    /// </para>
    /// </remarks>
    public class ThemeManager : INotifyPropertyChanged
    {
        /// <summary>
        /// Singleton instance of the ThemeManager.
        /// </summary>
        private static ThemeManager? _instance;
        
        /// <summary>
        /// Reference to the centralized configuration manager.
        /// </summary>
        private readonly CentralConfigurationManager _configManager;
        
        /// <summary>
        /// Current applied theme mode.
        /// </summary>
        private ThemeMode _currentTheme = ThemeMode.System;
        
        /// <summary>
        /// Whether transparency effects are enabled.
        /// </summary>
        private bool _transparencyEnabled = true;
        
        /// <summary>
        /// Background opacity level (0.0 to 1.0).
        /// </summary>
        private double _backgroundOpacity = 0.95;
        
        /// <summary>
        /// Whether blur effects are enabled.
        /// </summary>
        private bool _blurEffectsEnabled = true;
        
        /// <summary>
        /// Gets the singleton instance of the ThemeManager.
        /// </summary>
        public static ThemeManager Instance => _instance ??= new ThemeManager();
        
        /// <summary>
        /// Gets or sets the current theme mode.
        /// </summary>
        public ThemeMode CurrentTheme
        {
            get => _currentTheme;
            set { _currentTheme = value; OnPropertyChanged(); ApplyTheme(); }
        }
        
        /// <summary>
        /// Gets or sets whether transparency effects are enabled.
        /// </summary>
        public bool TransparencyEnabled
        {
            get => _transparencyEnabled;
            set 
            { 
                if (_transparencyEnabled != value)
                {
                    _transparencyEnabled = value; 
                    
                    // Save to configuration
                    _configManager.Configuration.UISettings.EnableTransparencyEffects = value;
                    _configManager.SaveConfiguration();
                    
                    // Apply effects and notify components
                    ApplyTransparencyEffects();
                    NotifyDockTransparencyChanged();
                    OnPropertyChanged(); 
                }
            }
        }
        
        /// <summary>
        /// Gets or sets the background opacity level (0.0 to 1.0).
        /// </summary>
        public double BackgroundOpacity
        {
            get => _backgroundOpacity;
            set { _backgroundOpacity = Math.Max(0.0, Math.Min(1.0, value)); OnPropertyChanged(); ApplyTransparencyEffects(); }
        }
        
        /// <summary>
        /// Gets or sets whether blur effects are enabled.
        /// </summary>
        public bool BlurEffectsEnabled
        {
            get => _blurEffectsEnabled;
            set { _blurEffectsEnabled = value; OnPropertyChanged(); ApplyBlurEffects(); }
        }
        
        /// <summary>
        /// Gets whether the effective theme is dark (either explicitly set or system is dark).
        /// </summary>
        public bool IsDarkTheme
        {
            get
            {
                return _currentTheme switch
                {
                    ThemeMode.Dark => true,
                    ThemeMode.Light => false,
                    ThemeMode.System => IsSystemDarkTheme(),
                    _ => false
                };
            }
        }
        
        private ThemeManager()
        {
            _configManager = CentralConfigurationManager.Instance;
            LoadThemeSettings();
            
            // Listen for system theme changes
            SystemEvents.UserPreferenceChanged += OnSystemPreferenceChanged;
            
            // Subscribe to configuration changes
            _configManager.PropertyChanged += OnConfigurationChanged;
        }
        
        /// <summary>
        /// Loads theme settings from the centralized configuration.
        /// </summary>
        public void LoadThemeSettings()
        {
            var config = _configManager.Configuration;
            
            _currentTheme = Enum.TryParse<ThemeMode>(config.AppSettings.Theme, out var theme) ? theme : ThemeMode.System;
            _transparencyEnabled = config.UISettings.EnableTransparencyEffects;
            _backgroundOpacity = config.UISettings.BackgroundOpacity;
            _blurEffectsEnabled = config.UISettings.EnableBlurEffect;
            
            ApplyTheme();
        }
        
        /// <summary>
        /// Applies the current theme to all application windows and resources.
        /// </summary>
        public void ApplyTheme()
        {
            try
            {
                var isDark = IsDarkTheme;
                
                // Update application resources
                Application.Current.Resources.MergedDictionaries.Clear();
                
                // Load base styles
                var baseStyles = new ResourceDictionary { Source = new Uri("pack://application:,,,/Styles/SeroStyles.xaml") };
                Application.Current.Resources.MergedDictionaries.Add(baseStyles);
                
                // Load theme-specific styles
                var themeStyles = new ResourceDictionary
                {
                    Source = new Uri($"pack://application:,,,/Styles/{(isDark ? "DarkTheme" : "LightTheme")}.xaml")
                };
                Application.Current.Resources.MergedDictionaries.Add(themeStyles);
                
                // Load animations and effects
                var animations = new ResourceDictionary { Source = new Uri("pack://application:,,,/Styles/Animations.xaml") };
                Application.Current.Resources.MergedDictionaries.Add(animations);
                
                // Apply transparency and blur effects
                ApplyTransparencyEffects();
                ApplyBlurEffects();
                
                // Notify theme change
                OnPropertyChanged(nameof(IsDarkTheme));
                ThemeChanged?.Invoke(isDark);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying theme: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Applies transparency effects to dock components by updating resource dictionaries.
        /// </summary>
        private void ApplyTransparencyEffects()
        {
            try
            {
                var resourceKey = _transparencyEnabled ? "DockBackgroundBrush" : "DockBackgroundNoTransparencyBrush";
                var resource = Application.Current.FindResource(resourceKey);
                
                if (resource != null)
                {
                    // Update the DockBackgroundBrush resource to use transparency or no transparency
                    Application.Current.Resources["DockBackgroundBrush"] = resource;
                    
                    System.Diagnostics.Debug.WriteLine($"Dock transparency updated: {(_transparencyEnabled ? "Enabled" : "Disabled")}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying transparency effects: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Applies blur effects to application windows.
        /// </summary>
        private void ApplyBlurEffects()
        {
            try
            {
                // Apply blur effects through WindowsIntegration if available
                foreach (Window window in Application.Current.Windows)
                {
                    var hwnd = new System.Windows.Interop.WindowInteropHelper(window).Handle;
                    if (hwnd != IntPtr.Zero)
                    {
                        if (_blurEffectsEnabled)
                        {
                            // Enable blur effect through Windows API
                            Platform.WindowsIntegration.EnableBlurBehind(hwnd);
                        }
                        else
                        {
                            // Disable blur effect
                            Platform.WindowsIntegration.DisableBlurBehind(hwnd);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying blur effects: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Detects if the Windows system theme is currently dark.
        /// </summary>
        /// <returns>True if system is using dark theme, false otherwise.</returns>
        private bool IsSystemDarkTheme()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                var value = key?.GetValue("AppsUseLightTheme");
                return value is int intValue && intValue == 0;
            }
            catch
            {
                return false; // Default to light theme if detection fails
            }
        }
        
        /// <summary>
        /// Handles system preference changes to detect theme changes.
        /// </summary>
        private void OnSystemPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category == UserPreferenceCategory.General && _currentTheme == ThemeMode.System)
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    OnPropertyChanged(nameof(IsDarkTheme));
                    ApplyTheme();
                }));
            }
        }
        
        /// <summary>
        /// Handles configuration changes from CentralConfigurationManager.
        /// </summary>
        private void OnConfigurationChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CentralConfigurationManager.Configuration))
            {
                LoadThemeSettings();
            }
        }
        
        /// <summary>
        /// Event fired when theme changes.
        /// </summary>
        public event Action<bool>? ThemeChanged;
        
        /// <summary>
        /// Notifies dock components about transparency changes.
        /// </summary>
        private void NotifyDockTransparencyChanged()
        {
            try
            {
                // Find all dock windows and force them to refresh their styles
                foreach (Window window in Application.Current.Windows)
                {
                    if (window is Views.DockWindow dockWindow)
                    {
                        // Force the dock to rebind to the updated resource
                        var dock = dockWindow.FindName("Dock") as Views.SeroDock;
                        dock?.InvalidateVisual();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error notifying dock transparency change: {ex.Message}");
            }
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    
    /// <summary>
    /// Available theme modes for the application.
    /// </summary>
    public enum ThemeMode
    {
        /// <summary>Follow system theme preference.</summary>
        System,
        /// <summary>Always use light theme.</summary>
        Light,
        /// <summary>Always use dark theme.</summary>
        Dark
    }
}