using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Microsoft.Win32;
using SeroDesk.Services;

namespace SeroDesk.Views
{
    /// <summary>
    /// Windows 11-style settings window for SeroDesk configuration
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private readonly CentralConfigurationManager _configManager;
        
        public SettingsWindow()
        {
            InitializeComponent();
            _configManager = CentralConfigurationManager.Instance;
            
            // Wait for the window to be fully loaded before starting animations
            Loaded += (s, e) => {
                LoadSettings();
                LoadSystemInfo();
                AnimateIn();
            };
        }

        private void LoadSettings()
        {
            try
            {
                // Load saved settings from centralized configuration
                _configManager.LoadConfiguration();
                var config = _configManager.Configuration;
                
                // General settings
                // Find and set language combo box
                var languageCombo = (ComboBox?)FindName("LanguageCombo");
                if (languageCombo?.Items != null)
                {
                    foreach (ComboBoxItem item in languageCombo.Items.OfType<ComboBoxItem>())
                    {
                        if (item.Content?.ToString() == config.AppSettings.Language)
                        {
                            item.IsSelected = true;
                            break;
                        }
                    }
                }
                
                // Set toggles with null checks
                var startWithWindowsToggle = (CheckBox?)FindName("StartWithWindowsToggle");
                if (startWithWindowsToggle != null)
                    startWithWindowsToggle.IsChecked = config.AppSettings.StartWithWindows;
                
                var shellReplacementToggle = (CheckBox?)FindName("ShellReplacementToggle");
                if (shellReplacementToggle != null)
                    shellReplacementToggle.IsChecked = config.AppSettings.ShellReplacementMode;
                
                // Appearance settings
                switch (config.AppSettings.Theme)
                {
                    case "Light":
                        if (ThemeLight != null) ThemeLight.IsChecked = true;
                        break;
                    case "Dark":
                        if (ThemeDark != null) ThemeDark.IsChecked = true;
                        break;
                    case "System":
                        if (ThemeSystem != null) ThemeSystem.IsChecked = true;
                        break;
                }
                
                var transparencyToggle = (CheckBox?)FindName("TransparencyToggle");
                if (transparencyToggle != null)
                    transparencyToggle.IsChecked = config.UISettings.EnableTransparencyEffects;
                
                var animationSpeedSlider = (Slider?)FindName("AnimationSpeedSlider");
                if (animationSpeedSlider != null)
                    animationSpeedSlider.Value = config.UISettings.AnimationSpeedMultiplier;
                
                // Dock settings
                switch (config.DockConfig.Position)
                {
                    case "Bottom":
                        var dockBottom = (RadioButton?)FindName("DockBottom");
                        if (dockBottom != null) dockBottom.IsChecked = true;
                        break;
                    case "Left":
                        var dockLeft = (RadioButton?)FindName("DockLeft");
                        if (dockLeft != null) dockLeft.IsChecked = true;
                        break;
                    case "Right":
                        var dockRight = (RadioButton?)FindName("DockRight");
                        if (dockRight != null) dockRight.IsChecked = true;
                        break;
                }
                
                var autoHideDockToggle = (CheckBox?)FindName("AutoHideDockToggle");
                if (autoHideDockToggle != null)
                    autoHideDockToggle.IsChecked = config.DockConfig.AutoHide;
                
                var dockIconSizeSlider = (Slider?)FindName("DockIconSizeSlider");
                if (dockIconSizeSlider != null)
                    dockIconSizeSlider.Value = config.DockConfig.IconSize;
                
                var showRecentAppsToggle = (CheckBox?)FindName("ShowRecentAppsToggle");
                if (showRecentAppsToggle != null)
                    showRecentAppsToggle.IsChecked = config.DockConfig.ShowRecentApps;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
                // Continue with default values if loading fails
            }
        }

        private void LoadSystemInfo()
        {
            try
            {
                // Windows version
                var os = Environment.OSVersion;
                WindowsVersion.Text = $"Windows {os.Version.Major}.{os.Version.Minor} (Build {os.Version.Build})";
                
                // .NET version
                DotNetVersion.Text = Environment.Version.ToString();
                
                // Memory usage
                var process = Process.GetCurrentProcess();
                var memoryMB = process.WorkingSet64 / (1024 * 1024);
                MemoryUsage.Text = $"{memoryMB:N0} MB";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading system info: {ex.Message}");
            }
        }

        private void AnimateIn()
        {
            try
            {
                // Use AnimationManager for consistent animation settings
                var animationManager = Services.AnimationManager.Instance;
                animationManager.ApplyFadeIn(this, 300);
            }
            catch (Exception ex)
            {
                // Fallback: Show window without animation
                System.Diagnostics.Debug.WriteLine($"Animation error: {ex.Message}");
                Opacity = 1;
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            // Save settings before closing
            SaveSettings();
            
            // Animate out using AnimationManager
            var animationManager = Services.AnimationManager.Instance;
            animationManager.ApplyFadeOut(this, 150, () => Close());
        }

        private void NavButton_Click(object sender, RoutedEventArgs e)
        {
            // Hide all panels
            GeneralSettings.Visibility = Visibility.Collapsed;
            AppearanceSettings.Visibility = Visibility.Collapsed;
            DockSettings.Visibility = Visibility.Collapsed;
            AboutSettings.Visibility = Visibility.Collapsed;
            
            // Show selected panel
            if (sender == NavGeneral)
                GeneralSettings.Visibility = Visibility.Visible;
            else if (sender == NavAppearance)
                AppearanceSettings.Visibility = Visibility.Visible;
            else if (sender == NavDock)
                DockSettings.Visibility = Visibility.Visible;
            else if (sender == NavAbout)
                AboutSettings.Visibility = Visibility.Visible;
            
            // TODO: Add panels for other navigation items
        }

        private void SaveSettings()
        {
            var config = _configManager.Configuration;
            
            // General settings - Language switching
            var languageCombo = (ComboBox)FindName("LanguageCombo");
            var selectedLanguage = ((ComboBoxItem?)languageCombo?.SelectedItem)?.Content?.ToString() ?? "English";
            var languageChanged = config.AppSettings.Language != selectedLanguage;
            
            config.AppSettings.Language = selectedLanguage;
            config.AppSettings.StartWithWindows = ((CheckBox)FindName("StartWithWindowsToggle"))?.IsChecked ?? false;
            config.AppSettings.ShellReplacementMode = ((CheckBox)FindName("ShellReplacementToggle"))?.IsChecked ?? false;
            
            // Appearance settings - Apply theme changes immediately
            var themeChanged = false;
            var currentTheme = config.AppSettings.Theme;
            
            if (ThemeLight.IsChecked == true && currentTheme != "Light")
            {
                config.AppSettings.Theme = "Light";
                themeChanged = true;
            }
            else if (ThemeDark.IsChecked == true && currentTheme != "Dark")
            {
                config.AppSettings.Theme = "Dark";
                themeChanged = true;
            }
            else if (ThemeSystem.IsChecked == true && currentTheme != "System")
            {
                config.AppSettings.Theme = "System";
                themeChanged = true;
            }
            
            // UI Effects settings
            var transparencyEnabled = ((CheckBox)FindName("TransparencyToggle"))?.IsChecked ?? true;
            var animationSpeed = ((Slider)FindName("AnimationSpeedSlider"))?.Value ?? 1.0;
            
            var transparencyChanged = config.UISettings.EnableTransparencyEffects != transparencyEnabled;
            var animationSpeedChanged = Math.Abs(config.UISettings.AnimationSpeedMultiplier - animationSpeed) > 0.01;
            
            config.UISettings.EnableTransparencyEffects = transparencyEnabled;
            config.UISettings.AnimationSpeedMultiplier = animationSpeed;
            
            // Dock settings - Track changes for immediate application
            var currentDockPosition = config.DockConfig.Position;
            var currentAutoHide = config.DockConfig.AutoHide;
            var currentIconSize = config.DockConfig.IconSize;
            var currentShowRecentApps = config.DockConfig.ShowRecentApps;
            
            var newDockPosition = "Bottom";
            if (((RadioButton)FindName("DockLeft"))?.IsChecked == true)
                newDockPosition = "Left";
            else if (((RadioButton)FindName("DockRight"))?.IsChecked == true)
                newDockPosition = "Right";
            
            var newAutoHide = ((CheckBox)FindName("AutoHideDockToggle"))?.IsChecked ?? false;
            var newIconSize = (int)(((Slider)FindName("DockIconSizeSlider"))?.Value ?? 48);
            var newShowRecentApps = ((CheckBox)FindName("ShowRecentAppsToggle"))?.IsChecked ?? true;
            
            var dockPositionChanged = currentDockPosition != newDockPosition;
            var autoHideChanged = currentAutoHide != newAutoHide;
            var iconSizeChanged = currentIconSize != newIconSize;
            var showRecentAppsChanged = currentShowRecentApps != newShowRecentApps;
            
            config.DockConfig.Position = newDockPosition;
            config.DockConfig.AutoHide = newAutoHide;
            config.DockConfig.IconSize = newIconSize;
            config.DockConfig.ShowRecentApps = newShowRecentApps;
            
            // Save configuration
            _configManager.SaveConfiguration();
            
            // Apply changes immediately through managers
            
            // 1. Language changes
            if (languageChanged)
            {
                var localizationManager = Services.LocalizationManager.Instance;
                localizationManager.CurrentLanguage = selectedLanguage;
            }
            
            // 2. Theme changes
            var themeManager = Services.ThemeManager.Instance;
            if (themeChanged)
            {
                if (Enum.TryParse<Services.ThemeMode>(config.AppSettings.Theme, out var themeMode))
                {
                    themeManager.CurrentTheme = themeMode;
                }
            }
            
            // 3. Transparency changes
            if (transparencyChanged)
            {
                themeManager.TransparencyEnabled = transparencyEnabled;
            }
            
            // 4. Animation changes
            if (animationSpeedChanged)
            {
                var animationManager = Services.AnimationManager.Instance;
                animationManager.SpeedMultiplier = animationSpeed;
            }
            
            // 5. Dock changes
            var dockManager = Services.DockManager.Instance;
            if (dockPositionChanged)
            {
                if (Enum.TryParse<Services.DockPosition>(newDockPosition, out var dockPos))
                {
                    dockManager.Position = dockPos;
                }
            }
            
            if (autoHideChanged)
            {
                dockManager.AutoHide = newAutoHide;
            }
            
            if (iconSizeChanged)
            {
                dockManager.IconSize = newIconSize;
            }
            
            if (showRecentAppsChanged)
            {
                dockManager.ShowRecentApps = newShowRecentApps;
            }
            
            // Update other theme manager properties
            themeManager.BackgroundOpacity = config.UISettings.BackgroundOpacity;
            themeManager.BlurEffectsEnabled = config.UISettings.EnableBlurEffect;
        }

        /// <summary>
        /// Shows the settings window centered on screen
        /// </summary>
        public static void ShowSettings()
        {
            var settingsWindow = new SettingsWindow();
            settingsWindow.ShowDialog();
        }
    }
}