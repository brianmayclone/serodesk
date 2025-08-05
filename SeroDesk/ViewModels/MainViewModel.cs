using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using SeroDesk.Models;
using SeroDesk.Services;

namespace SeroDesk.ViewModels
{
    /// <summary>
    /// Main ViewModel that coordinates the overall SeroDesk shell interface and manages global application state.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The MainViewModel serves as the central coordination point for the SeroDesk shell replacement.
    /// It manages:
    /// <list type="bullet">
    /// <item>LaunchPad functionality for application launching and organization</item>
    /// <item>Running applications collection and system tray integration</item>
    /// <item>Notification system and badge management</item>
    /// <item>System-wide settings like brightness and volume control</item>
    /// <item>Widget system coordination</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Important:</strong> Desktop functionality has been completely disabled in favor of
    /// the LaunchPad-only approach. All desktop-related properties and methods are marked as
    /// obsolete and should not be used in new code.
    /// </para>
    /// <para>
    /// The class follows the MVVM pattern and provides property change notifications for
    /// data binding with WPF user interface elements.
    /// </para>
    /// </remarks>
    public class MainViewModel : INotifyPropertyChanged
    {
        // Desktop functionality is DISABLED - only LaunchPad is used
        // private DesktopViewModel _desktopViewModel; // REMOVED
        private LaunchpadViewModel _launchpadViewModel;
        private bool _isDesktopVisible = true;
        private ObservableCollection<AppIcon> _runningApplications;
        private ObservableCollection<object> _notifications;
        private double _brightnessLevel = 75;
        private double _volumeLevel = 50;
        
        /// <summary>
        /// Gets an empty collection of desktop icons.
        /// </summary>
        /// <value>An empty observable collection.</value>
        /// <remarks>
        /// <strong>OBSOLETE:</strong> Desktop functionality has been completely disabled in SeroDesk.
        /// All application management is now handled through the LaunchPad interface.
        /// This property is kept for backward compatibility but always returns an empty collection.
        /// </remarks>
        [Obsolete("Desktop functionality is disabled. Use Launchpad instead.", false)]
        public ObservableCollection<AppIcon> DesktopIcons => new ObservableCollection<AppIcon>();
        
        /// <summary>
        /// Gets or sets the LaunchPad ViewModel that manages all application icons and groups.
        /// </summary>
        /// <value>The LaunchpadViewModel instance responsible for application management.</value>
        /// <remarks>
        /// This is the primary interface for managing applications in SeroDesk. The LaunchPad
        /// provides iOS-style application launching, organization, and search functionality.
        /// </remarks>
        public LaunchpadViewModel Launchpad
        {
            get => _launchpadViewModel;
            set { _launchpadViewModel = value; OnPropertyChanged(); }
        }
        
        /// <summary>
        /// Gets or sets a value indicating whether the desktop view is visible.
        /// </summary>
        /// <value>True if the desktop should be visible; otherwise, false.</value>
        /// <remarks>
        /// While desktop functionality is disabled, this property is maintained for UI state management
        /// and potential future use in view transitions.
        /// </remarks>
        public bool IsDesktopVisible
        {
            get => _isDesktopVisible;
            set { _isDesktopVisible = value; OnPropertyChanged(); }
        }
        
        /// <summary>
        /// Gets or sets the collection of currently running applications.
        /// </summary>
        /// <value>An observable collection of AppIcon instances representing active applications.</value>
        /// <remarks>
        /// This collection is used for taskbar-style functionality and application switching.
        /// It's automatically updated as applications are launched and closed.
        /// </remarks>
        public ObservableCollection<AppIcon> RunningApplications
        {
            get => _runningApplications;
            set { _runningApplications = value; OnPropertyChanged(); }
        }
        
        /// <summary>
        /// Gets or sets the collection of system notifications to display.
        /// </summary>
        /// <value>An observable collection containing notification objects.</value>
        /// <remarks>
        /// This collection supports the notification center functionality, displaying
        /// system alerts, messages, and application notifications to the user.
        /// </remarks>
        public ObservableCollection<object> Notifications
        {
            get => _notifications;
            set { _notifications = value; OnPropertyChanged(); }
        }
        
        /// <summary>
        /// Gets a value indicating whether there are unread notifications.
        /// </summary>
        /// <value>True if there are notifications in the collection; otherwise, false.</value>
        /// <remarks>
        /// This property is used for showing notification badges and indicators in the UI.
        /// </remarks>
        public bool HasNotifications => Notifications?.Count > 0;
        
        /// <summary>
        /// Gets or sets the current system brightness level as a percentage.
        /// </summary>
        /// <value>A double value between 0 and 100 representing the brightness percentage.</value>
        /// <remarks>
        /// This property integrates with system brightness controls and is displayed
        /// in the control center for quick adjustment.
        /// </remarks>
        public double BrightnessLevel
        {
            get => _brightnessLevel;
            set { _brightnessLevel = value; OnPropertyChanged(); }
        }
        
        /// <summary>
        /// Gets or sets the current system volume level as a percentage.
        /// </summary>
        /// <value>A double value between 0 and 100 representing the volume percentage.</value>
        /// <remarks>
        /// This property integrates with system volume controls and is displayed
        /// in the control center for quick adjustment.
        /// </remarks>
        public double VolumeLevel
        {
            get => _volumeLevel;
            set { _volumeLevel = value; OnPropertyChanged(); }
        }
        
        public MainViewModel()
        {
            // Desktop functionality is DISABLED - only LaunchPad is used
            // _desktopViewModel = new DesktopViewModel(); // REMOVED
            _launchpadViewModel = new LaunchpadViewModel();
            _runningApplications = new ObservableCollection<AppIcon>();
            _notifications = new ObservableCollection<object>();
        }
        
        /// <summary>
        /// Desktop icons are not used in this app - LaunchPad handles all apps
        /// </summary>
        [Obsolete("Desktop functionality is disabled. Use Launchpad.LoadAllApplicationsAsync() instead.")]
        public void LoadDesktopIcons()
        {
            // Desktop icons are not used in this app - LaunchPad handles all apps
            // Method kept for compatibility but does nothing
            System.Diagnostics.Debug.WriteLine("LoadDesktopIcons called but Desktop functionality is disabled");
        }
        
        public async void LoadAllAppsForSpringBoard()
        {
            // Only load for Launchpad - Desktop icons are not used in this app
            // await Desktop.LoadAllApplicationsAsync(); // DISABLED
            await Launchpad.LoadAllApplicationsAsync();
        }
        
        public void LoadWidgets(Canvas widgetContainer)
        {
            // Initialize Widget Manager with container
            WidgetManager.Instance.Initialize(widgetContainer);
        }
        
        public void ToggleDesktop()
        {
            IsDesktopVisible = !IsDesktopVisible;
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}