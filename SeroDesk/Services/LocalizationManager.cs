using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;

namespace SeroDesk.Services
{
    /// <summary>
    /// Manages application localization and language switching for SeroDesk.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The LocalizationManager provides centralized localization management that:
    /// <list type="bullet">
    /// <item>Supports multiple languages with resource dictionaries</item>
    /// <item>Enables runtime language switching without restart</item>
    /// <item>Manages culture settings for proper date/time/number formatting</item>
    /// <item>Provides translated strings for UI elements</item>
    /// <item>Coordinates with CentralConfigurationManager for persistence</item>
    /// </list>
    /// </para>
    /// <para>
    /// Languages are loaded from ResourceDictionaries and can be switched
    /// at runtime by updating the application's merged dictionaries.
    /// </para>
    /// </remarks>
    public class LocalizationManager : INotifyPropertyChanged
    {
        /// <summary>
        /// Singleton instance of the LocalizationManager.
        /// </summary>
        private static LocalizationManager? _instance;
        
        /// <summary>
        /// Reference to the centralized configuration manager.
        /// </summary>
        private readonly CentralConfigurationManager _configManager;
        
        /// <summary>
        /// Current application language.
        /// </summary>
        private string _currentLanguage = "English";
        
        /// <summary>
        /// Available languages in the application.
        /// </summary>
        private readonly Dictionary<string, LanguageInfo> _availableLanguages;
        
        /// <summary>
        /// Gets the singleton instance of the LocalizationManager.
        /// </summary>
        public static LocalizationManager Instance => _instance ??= new LocalizationManager();
        
        /// <summary>
        /// Gets or sets the current application language.
        /// </summary>
        public string CurrentLanguage
        {
            get => _currentLanguage;
            set { SetLanguage(value); OnPropertyChanged(); }
        }
        
        /// <summary>
        /// Gets the available languages in the application.
        /// </summary>
        public IReadOnlyDictionary<string, LanguageInfo> AvailableLanguages => _availableLanguages;
        
        /// <summary>
        /// Gets the current culture info for the selected language.
        /// </summary>
        public CultureInfo CurrentCulture { get; private set; }
        
        private LocalizationManager()
        {
            _configManager = CentralConfigurationManager.Instance;
            CurrentCulture = CultureInfo.CurrentCulture;
            
            // Initialize available languages
            _availableLanguages = new Dictionary<string, LanguageInfo>
            {
                ["English"] = new LanguageInfo("English", "en-US", "🇺🇸"),
                ["Deutsch"] = new LanguageInfo("Deutsch", "de-DE", "🇩🇪"),
                ["Français"] = new LanguageInfo("Français", "fr-FR", "🇫🇷"),
                ["Español"] = new LanguageInfo("Español", "es-ES", "🇪🇸"),
                ["Italiano"] = new LanguageInfo("Italiano", "it-IT", "🇮🇹"),
                ["日本語"] = new LanguageInfo("日本語", "ja-JP", "🇯🇵"),
                ["中文"] = new LanguageInfo("中文", "zh-CN", "🇨🇳"),
                ["Русский"] = new LanguageInfo("Русский", "ru-RU", "🇷🇺")
            };
            
            LoadLanguageSettings();
            
            // Subscribe to configuration changes
            _configManager.PropertyChanged += OnConfigurationChanged;
        }
        
        /// <summary>
        /// Loads language settings from the centralized configuration.
        /// </summary>
        public void LoadLanguageSettings()
        {
            var config = _configManager.Configuration;
            var savedLanguage = config.AppSettings.Language;
            
            if (_availableLanguages.ContainsKey(savedLanguage))
            {
                SetLanguage(savedLanguage, false); // Don't save during initial load
            }
            else
            {
                // Default to English if saved language is not available
                SetLanguage("English", false);
            }
        }
        
        /// <summary>
        /// Sets the application language and applies localization changes.
        /// </summary>
        /// <param name="languageName">The name of the language to set.</param>
        /// <param name="saveToConfig">Whether to save the language change to configuration.</param>
        public void SetLanguage(string languageName, bool saveToConfig = true)
        {
            if (!_availableLanguages.TryGetValue(languageName, out var languageInfo))
            {
                System.Diagnostics.Debug.WriteLine($"Language '{languageName}' not found. Using English as fallback.");
                languageName = "English";
                languageInfo = _availableLanguages["English"];
            }
            
            _currentLanguage = languageName;
            
            try
            {
                // Set culture for proper formatting
                CurrentCulture = new CultureInfo(languageInfo.CultureCode);
                CultureInfo.CurrentCulture = CurrentCulture;
                CultureInfo.CurrentUICulture = CurrentCulture;
                
                // Apply language resources
                ApplyLanguageResources(languageInfo);
                
                if (saveToConfig)
                {
                    // Save to configuration
                    var config = _configManager.Configuration;
                    config.AppSettings.Language = languageName;
                    _configManager.SaveConfiguration();
                }
                
                // Notify language change
                LanguageChanged?.Invoke(languageName, languageInfo);
                OnPropertyChanged(nameof(CurrentCulture));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting language to {languageName}: {ex.Message}");
                // Fallback to English on error
                if (languageName != "English")
                {
                    SetLanguage("English", saveToConfig);
                }
            }
        }
        
        /// <summary>
        /// Applies language resources to the application.
        /// </summary>
        /// <param name="languageInfo">The language information to apply.</param>
        private void ApplyLanguageResources(LanguageInfo languageInfo)
        {
            try
            {
                // Remove existing language resources
                var resourcesToRemove = new List<ResourceDictionary>();
                foreach (var dict in Application.Current.Resources.MergedDictionaries)
                {
                    if (dict.Source?.OriginalString?.Contains("/Languages/") == true)
                    {
                        resourcesToRemove.Add(dict);
                    }
                }
                
                foreach (var dict in resourcesToRemove)
                {
                    Application.Current.Resources.MergedDictionaries.Remove(dict);
                }
                
                // Load new language resources
                var languageResourceUri = $"pack://application:,,,/Resources/Languages/{languageInfo.CultureCode}.xaml";
                
                try
                {
                    var languageDict = new ResourceDictionary { Source = new Uri(languageResourceUri) };
                    Application.Current.Resources.MergedDictionaries.Add(languageDict);
                }
                catch (Exception)
                {
                    // If specific language file doesn't exist, create basic resources
                    CreateBasicLanguageResources(languageInfo);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying language resources: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Creates basic language resources when specific language files are not available.
        /// </summary>
        /// <param name="languageInfo">The language information to create resources for.</param>
        private void CreateBasicLanguageResources(LanguageInfo languageInfo)
        {
            var basicResources = new ResourceDictionary();
            
            // Add basic translations based on language
            switch (languageInfo.CultureCode)
            {
                case "de-DE":
                    AddGermanResources(basicResources);
                    break;
                case "fr-FR":
                    AddFrenchResources(basicResources);
                    break;
                case "es-ES":
                    AddSpanishResources(basicResources);
                    break;
                case "it-IT":
                    AddItalianResources(basicResources);
                    break;
                default:
                    AddEnglishResources(basicResources);
                    break;
            }
            
            Application.Current.Resources.MergedDictionaries.Add(basicResources);
        }
        
        private void AddEnglishResources(ResourceDictionary resources)
        {
            resources["Settings"] = "Settings";
            resources["General"] = "General";
            resources["Appearance"] = "Appearance";
            resources["Dock"] = "Dock";
            resources["About"] = "About";
            resources["Language"] = "Language";
            resources["Theme"] = "Theme";
            resources["Light"] = "Light";
            resources["Dark"] = "Dark";
            resources["System"] = "System";
            resources["Transparency"] = "Transparency";
            resources["AnimationSpeed"] = "Animation Speed";
            resources["Position"] = "Position";
            resources["AutoHide"] = "Auto Hide";
            resources["IconSize"] = "Icon Size";
            resources["ShowRecentApps"] = "Show Recent Apps";
            resources["Close"] = "Close";
            resources["StartWithWindows"] = "Start with Windows";
            resources["ShellReplacement"] = "Shell Replacement Mode";
        }
        
        private void AddGermanResources(ResourceDictionary resources)
        {
            resources["Settings"] = "Einstellungen";
            resources["General"] = "Allgemein";
            resources["Appearance"] = "Darstellung";
            resources["Dock"] = "Dock";
            resources["About"] = "Über";
            resources["Language"] = "Sprache";
            resources["Theme"] = "Design";
            resources["Light"] = "Hell";
            resources["Dark"] = "Dunkel";
            resources["System"] = "System";
            resources["Transparency"] = "Transparenz";
            resources["AnimationSpeed"] = "Animationsgeschwindigkeit";
            resources["Position"] = "Position";
            resources["AutoHide"] = "Automatisch ausblenden";
            resources["IconSize"] = "Symbolgröße";
            resources["ShowRecentApps"] = "Zuletzt verwendete Apps anzeigen";
            resources["Close"] = "Schließen";
            resources["StartWithWindows"] = "Mit Windows starten";
            resources["ShellReplacement"] = "Shell-Ersatz Modus";
        }
        
        private void AddFrenchResources(ResourceDictionary resources)
        {
            resources["Settings"] = "Paramètres";
            resources["General"] = "Général";
            resources["Appearance"] = "Apparence";
            resources["Dock"] = "Dock";
            resources["About"] = "À propos";
            resources["Language"] = "Langue";
            resources["Theme"] = "Thème";
            resources["Light"] = "Clair";
            resources["Dark"] = "Sombre";
            resources["System"] = "Système";
            resources["Transparency"] = "Transparence";
            resources["AnimationSpeed"] = "Vitesse d'animation";
            resources["Position"] = "Position";
            resources["AutoHide"] = "Masquage automatique";
            resources["IconSize"] = "Taille des icônes";
            resources["ShowRecentApps"] = "Afficher les apps récentes";
            resources["Close"] = "Fermer";
            resources["StartWithWindows"] = "Démarrer avec Windows";
            resources["ShellReplacement"] = "Mode de remplacement Shell";
        }
        
        private void AddSpanishResources(ResourceDictionary resources)
        {
            resources["Settings"] = "Configuración";
            resources["General"] = "General";
            resources["Appearance"] = "Apariencia";
            resources["Dock"] = "Dock";
            resources["About"] = "Acerca de";
            resources["Language"] = "Idioma";
            resources["Theme"] = "Tema";
            resources["Light"] = "Claro";
            resources["Dark"] = "Oscuro";
            resources["System"] = "Sistema";
            resources["Transparency"] = "Transparencia";
            resources["AnimationSpeed"] = "Velocidad de animación";
            resources["Position"] = "Posición";
            resources["AutoHide"] = "Ocultar automáticamente";
            resources["IconSize"] = "Tamaño de iconos";
            resources["ShowRecentApps"] = "Mostrar apps recientes";
            resources["Close"] = "Cerrar";
            resources["StartWithWindows"] = "Iniciar con Windows";
            resources["ShellReplacement"] = "Modo de reemplazo Shell";
        }
        
        private void AddItalianResources(ResourceDictionary resources)
        {
            resources["Settings"] = "Impostazioni";
            resources["General"] = "Generale";
            resources["Appearance"] = "Aspetto";
            resources["Dock"] = "Dock";
            resources["About"] = "Info";
            resources["Language"] = "Lingua";
            resources["Theme"] = "Tema";
            resources["Light"] = "Chiaro";
            resources["Dark"] = "Scuro";
            resources["System"] = "Sistema";
            resources["Transparency"] = "Trasparenza";
            resources["AnimationSpeed"] = "Velocità animazione";
            resources["Position"] = "Posizione";
            resources["AutoHide"] = "Nascondi automaticamente";
            resources["IconSize"] = "Dimensione icone";
            resources["ShowRecentApps"] = "Mostra app recenti";
            resources["Close"] = "Chiudi";
            resources["StartWithWindows"] = "Avvia con Windows";
            resources["ShellReplacement"] = "Modalità sostituzione Shell";
        }
        
        /// <summary>
        /// Gets a localized string by key.
        /// </summary>
        /// <param name="key">The resource key to look up.</param>
        /// <returns>The localized string, or the key if not found.</returns>
        public string GetString(string key)
        {
            try
            {
                var resource = Application.Current.Resources[key];
                return resource?.ToString() ?? key;
            }
            catch
            {
                return key;
            }
        }
        
        /// <summary>
        /// Handles configuration changes from CentralConfigurationManager.
        /// </summary>
        private void OnConfigurationChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CentralConfigurationManager.Configuration))
            {
                LoadLanguageSettings();
            }
        }
        
        /// <summary>
        /// Event fired when language changes.
        /// </summary>
        public event Action<string, LanguageInfo>? LanguageChanged;
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    
    /// <summary>
    /// Information about a supported language.
    /// </summary>
    public class LanguageInfo
    {
        /// <summary>
        /// Gets the display name of the language.
        /// </summary>
        public string DisplayName { get; }
        
        /// <summary>
        /// Gets the culture code (e.g., "en-US", "de-DE").
        /// </summary>
        public string CultureCode { get; }
        
        /// <summary>
        /// Gets the flag emoji for the language.
        /// </summary>
        public string FlagEmoji { get; }
        
        public LanguageInfo(string displayName, string cultureCode, string flagEmoji)
        {
            DisplayName = displayName;
            CultureCode = cultureCode;
            FlagEmoji = flagEmoji;
        }
        
        public override string ToString() => $"{FlagEmoji} {DisplayName}";
    }
}