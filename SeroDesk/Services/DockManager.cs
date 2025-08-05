using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;

namespace SeroDesk.Services
{
    /// <summary>
    /// Manages dock configuration and applies settings changes dynamically to all dock instances.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The DockManager provides centralized dock management that:
    /// <list type="bullet">
    /// <item>Controls dock position (Bottom, Left, Right)</item>
    /// <item>Manages auto-hide behavior</item>
    /// <item>Adjusts icon sizes dynamically</item>
    /// <item>Applies spacing and scale settings</item>
    /// <item>Coordinates with CentralConfigurationManager for persistence</item>
    /// </list>
    /// </para>
    /// <para>
    /// Changes are applied immediately to all active dock windows without
    /// requiring application restart or window recreation.
    /// </para>
    /// </remarks>
    public class DockManager : INotifyPropertyChanged
    {
        /// <summary>
        /// Singleton instance of the DockManager.
        /// </summary>
        private static DockManager? _instance;
        
        /// <summary>
        /// Reference to the centralized configuration manager.
        /// </summary>
        private readonly CentralConfigurationManager _configManager;
        
        /// <summary>
        /// Current dock position.
        /// </summary>
        private DockPosition _position = DockPosition.Bottom;
        
        /// <summary>
        /// Whether auto-hide is enabled.
        /// </summary>
        private bool _autoHide = false;
        
        /// <summary>
        /// Current icon size in pixels.
        /// </summary>
        private int _iconSize = 48;
        
        /// <summary>
        /// Icon scale multiplier.
        /// </summary>
        private double _iconScale = 1.0;
        
        /// <summary>
        /// Icon spacing in pixels.
        /// </summary>
        private int _iconSpacing = 20;
        
        /// <summary>
        /// Whether to show recent applications.
        /// </summary>
        private bool _showRecentApps = true;
        
        /// <summary>
        /// Gets the singleton instance of the DockManager.
        /// </summary>
        public static DockManager Instance => _instance ??= new DockManager();
        
        /// <summary>
        /// Gets or sets the dock position.
        /// </summary>
        public DockPosition Position
        {
            get => _position;
            set { _position = value; OnPropertyChanged(); ApplyDockPosition(); }
        }
        
        /// <summary>
        /// Gets or sets whether auto-hide is enabled.
        /// </summary>
        public bool AutoHide
        {
            get => _autoHide;
            set { _autoHide = value; OnPropertyChanged(); ApplyAutoHide(); }
        }
        
        /// <summary>
        /// Gets or sets the icon size in pixels.
        /// </summary>
        public int IconSize
        {
            get => _iconSize;
            set { _iconSize = Math.Max(32, Math.Min(64, value)); OnPropertyChanged(); ApplyIconSize(); }
        }
        
        /// <summary>
        /// Gets or sets the icon scale multiplier.
        /// </summary>
        public double IconScale
        {
            get => _iconScale;
            set { _iconScale = Math.Max(0.5, Math.Min(2.0, value)); OnPropertyChanged(); ApplyIconScale(); }
        }
        
        /// <summary>
        /// Gets or sets the icon spacing in pixels.
        /// </summary>
        public int IconSpacing
        {
            get => _iconSpacing;
            set { _iconSpacing = Math.Max(10, Math.Min(50, value)); OnPropertyChanged(); ApplyIconSpacing(); }
        }
        
        /// <summary>
        /// Gets or sets whether to show recent applications.
        /// </summary>
        public bool ShowRecentApps
        {
            get => _showRecentApps;
            set { _showRecentApps = value; OnPropertyChanged(); ApplyShowRecentApps(); }
        }
        
        private DockManager()
        {
            _configManager = CentralConfigurationManager.Instance;
            LoadDockSettings();
            
            // Subscribe to configuration changes
            _configManager.PropertyChanged += OnConfigurationChanged;
        }
        
        /// <summary>
        /// Loads dock settings from the centralized configuration.
        /// </summary>
        public void LoadDockSettings()
        {
            var config = _configManager.Configuration;
            var dockConfig = config.DockConfig;
            
            // Load settings without triggering apply methods
            _position = Enum.TryParse<DockPosition>(dockConfig.Position, out var pos) ? pos : DockPosition.Bottom;
            _autoHide = dockConfig.AutoHide;
            _iconSize = dockConfig.IconSize;
            _iconScale = dockConfig.IconScale;
            _iconSpacing = dockConfig.IconSpacing;
            _showRecentApps = dockConfig.ShowRecentApps;
            
            // Notify property changes
            OnPropertyChanged(nameof(Position));
            OnPropertyChanged(nameof(AutoHide));
            OnPropertyChanged(nameof(IconSize));
            OnPropertyChanged(nameof(IconScale));
            OnPropertyChanged(nameof(IconSpacing));
            OnPropertyChanged(nameof(ShowRecentApps));
            
            // Apply all settings to existing docks
            ApplyAllSettings();
        }
        
        /// <summary>
        /// Applies dock position changes to all active dock windows.
        /// </summary>
        private void ApplyDockPosition()
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                foreach (var window in Application.Current.Windows.OfType<Views.DockWindow>())
                {
                    ApplyPositionToWindow(window);
                }
            }));
        }
        
        /// <summary>
        /// Applies position settings to a specific dock window.
        /// </summary>
        /// <param name="dockWindow">The dock window to update.</param>
        private void ApplyPositionToWindow(Views.DockWindow dockWindow)
        {
            try
            {
                switch (_position)
                {
                    case DockPosition.Bottom:
                        dockWindow.WindowStartupLocation = WindowStartupLocation.Manual;
                        dockWindow.Left = 0;
                        dockWindow.Top = SystemParameters.PrimaryScreenHeight - dockWindow.Height;
                        dockWindow.Width = SystemParameters.PrimaryScreenWidth;
                        break;
                        
                    case DockPosition.Left:
                        dockWindow.WindowStartupLocation = WindowStartupLocation.Manual;
                        dockWindow.Left = 0;
                        dockWindow.Top = 0;
                        dockWindow.Width = _iconSize + 20; // Icon size plus padding
                        dockWindow.Height = SystemParameters.PrimaryScreenHeight;
                        break;
                        
                    case DockPosition.Right:
                        dockWindow.WindowStartupLocation = WindowStartupLocation.Manual;
                        dockWindow.Left = SystemParameters.PrimaryScreenWidth - (_iconSize + 20);
                        dockWindow.Top = 0;
                        dockWindow.Width = _iconSize + 20; // Icon size plus padding
                        dockWindow.Height = SystemParameters.PrimaryScreenHeight;
                        break;
                }
                
                // Update dock orientation if the dock control supports it
                var dock = dockWindow.FindName("Dock") as Views.SeroDock;
                dock?.UpdateOrientation(_position);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying dock position: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Applies auto-hide settings to all active dock windows.
        /// </summary>
        private void ApplyAutoHide()
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                foreach (var window in Application.Current.Windows.OfType<Views.DockWindow>())
                {
                    try
                    {
                        var dock = window.FindName("Dock") as Views.SeroDock;
                        dock?.SetAutoHide(_autoHide);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error applying auto-hide: {ex.Message}");
                    }
                }
            }));
        }
        
        /// <summary>
        /// Applies icon size changes to all active dock windows.
        /// </summary>
        private void ApplyIconSize()
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                foreach (var window in Application.Current.Windows.OfType<Views.DockWindow>())
                {
                    try
                    {
                        var dock = window.FindName("Dock") as Views.SeroDock;
                        dock?.SetIconSize(_iconSize);
                        
                        // Update window size for side docks
                        if (_position == DockPosition.Left || _position == DockPosition.Right)
                        {
                            window.Width = _iconSize + 20;
                            if (_position == DockPosition.Right)
                            {
                                window.Left = SystemParameters.PrimaryScreenWidth - window.Width;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error applying icon size: {ex.Message}");
                    }
                }
            }));
        }
        
        /// <summary>
        /// Applies icon scale changes to all active dock windows.
        /// </summary>
        private void ApplyIconScale()
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                foreach (var window in Application.Current.Windows.OfType<Views.DockWindow>())
                {
                    try
                    {
                        var dock = window.FindName("Dock") as Views.SeroDock;
                        dock?.SetIconScale(_iconScale);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error applying icon scale: {ex.Message}");
                    }
                }
            }));
        }
        
        /// <summary>
        /// Applies icon spacing changes to all active dock windows.
        /// </summary>
        private void ApplyIconSpacing()
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                foreach (var window in Application.Current.Windows.OfType<Views.DockWindow>())
                {
                    try
                    {
                        var dock = window.FindName("Dock") as Views.SeroDock;
                        dock?.SetIconSpacing(_iconSpacing);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error applying icon spacing: {ex.Message}");
                    }
                }
            }));
        }
        
        /// <summary>
        /// Applies show recent apps setting to all active dock windows.
        /// </summary>
        private void ApplyShowRecentApps()
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                foreach (var window in Application.Current.Windows.OfType<Views.DockWindow>())
                {
                    try
                    {
                        var dock = window.FindName("Dock") as Views.SeroDock;
                        dock?.SetShowRecentApps(_showRecentApps);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error applying show recent apps: {ex.Message}");
                    }
                }
            }));
        }
        
        /// <summary>
        /// Applies all dock settings to existing dock windows.
        /// </summary>
        private void ApplyAllSettings()
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                foreach (var window in Application.Current.Windows.OfType<Views.DockWindow>())
                {
                    try
                    {
                        ApplyPositionToWindow(window);
                        
                        var dock = window.FindName("Dock") as Views.SeroDock;
                        if (dock != null)
                        {
                            dock.SetAutoHide(_autoHide);
                            dock.SetIconSize(_iconSize);
                            dock.SetIconScale(_iconScale);
                            dock.SetIconSpacing(_iconSpacing);
                            dock.SetShowRecentApps(_showRecentApps);
                            dock.UpdateOrientation(_position);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error applying all dock settings: {ex.Message}");
                    }
                }
            }));
        }
        
        /// <summary>
        /// Registers a new dock window with the manager.
        /// </summary>
        /// <param name="dockWindow">The dock window to register.</param>
        public void RegisterDockWindow(Views.DockWindow dockWindow)
        {
            // Apply current settings to the new dock window
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    ApplyPositionToWindow(dockWindow);
                    
                    var dock = dockWindow.FindName("Dock") as Views.SeroDock;
                    if (dock != null)
                    {
                        dock.SetAutoHide(_autoHide);
                        dock.SetIconSize(_iconSize);
                        dock.SetIconScale(_iconScale);
                        dock.SetIconSpacing(_iconSpacing);
                        dock.SetShowRecentApps(_showRecentApps);
                        dock.UpdateOrientation(_position);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error registering dock window: {ex.Message}");
                }
            }));
        }
        
        /// <summary>
        /// Handles configuration changes from CentralConfigurationManager.
        /// </summary>
        private void OnConfigurationChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CentralConfigurationManager.Configuration))
            {
                LoadDockSettings();
            }
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    
    /// <summary>
    /// Available dock positions.
    /// </summary>
    public enum DockPosition
    {
        /// <summary>Bottom of the screen (default).</summary>
        Bottom,
        /// <summary>Left side of the screen.</summary>
        Left,
        /// <summary>Right side of the screen.</summary>
        Right
    }
}