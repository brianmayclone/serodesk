using System.IO;
using System.Windows;
using System.Windows.Threading;
using SeroDesk.Services;
using SeroDesk.Platform;

namespace SeroDesk
{
    /// <summary>
    /// Main Application class for SeroDesk - Touch-optimized Windows 11 Shell Extension.
    /// This class handles application lifecycle, exception management, and service initialization.
    /// SeroDesk replaces the Windows Explorer shell to provide an iOS-inspired touch interface.
    /// </summary>
    /// <remarks>
    /// The App class is responsible for:
    /// - Ensuring single instance execution to prevent multiple shell replacements
    /// - Initializing platform-specific Windows integration services
    /// - Managing the Explorer.exe process replacement for true shell functionality
    /// - Handling global exception scenarios and crash logging
    /// - Coordinating service startup and shutdown sequences
    /// </remarks>
    public partial class App : Application
    {
        /// <summary>
        /// Mutex for ensuring only one instance of SeroDesk runs at a time.
        /// Critical for shell replacement scenarios where multiple instances
        /// could cause system instability and conflict with Windows Explorer.
        /// </summary>
        private static Mutex? _mutex;
        
        /// <summary>
        /// Timer to continuously monitor and hide the Windows taskbar
        /// </summary>
        private static DispatcherTimer? _taskbarHideTimer;
        
        /// <summary>
        /// Global keyboard hook for Windows key functionality
        /// </summary>
        private static GlobalKeyboardHook? _globalKeyboardHook;
        
        /// <summary>
        /// Overrides the application startup event to initialize SeroDesk as a shell replacement.
        /// This method implements critical startup logic including single-instance enforcement,
        /// exception handling setup, and service initialization for the shell environment.
        /// </summary>
        /// <param name="e">Startup event arguments containing command line parameters and startup URI</param>
        /// <remarks>
        /// Startup sequence:
        /// 1. Creates a named mutex to prevent multiple instances
        /// 2. Sets up global exception handlers for crash recovery
        /// 3. Initializes Windows platform integration services
        /// 4. Terminates Explorer.exe to assume shell responsibilities
        /// 5. Starts background services for widget management, notifications, etc.
        /// </remarks>
        protected override void OnStartup(StartupEventArgs e)
        {
            // Ensure single instance - Critical for shell replacement stability
            // Using a GUID-based mutex name to avoid conflicts with other applications
            const string appName = "SeroDesk-{8F6F0AC4-B9A1-45fd-A8CF-72F04E6BDE8F}";
            _mutex = new Mutex(true, appName, out bool createdNew);
            
            if (!createdNew)
            {
                // Another instance is already running - inform user and exit gracefully
                MessageBox.Show("SeroDesk is already running.", "SeroDesk", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                Shutdown();
                return;
            }
            
            // Setup comprehensive exception handling for production stability
            // This ensures crashes are logged and the system can recover gracefully
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            
            // Initialize all core services required for shell operation
            InitializeServices();
            
            base.OnStartup(e);
        }
        
        /// <summary>
        /// Initializes all core services required for SeroDesk shell functionality.
        /// This method coordinates the startup of platform integration, Explorer replacement,
        /// background services, and user settings management in the correct sequence.
        /// </summary>
        /// <remarks>
        /// Service initialization order is critical:
        /// 1. WindowsIntegration - Sets up Win32 API wrappers and DPI awareness
        /// 2. ExplorerManager - Safely terminates Explorer.exe and assumes shell role
        /// 3. ServiceManager - Starts background services (notifications, widgets, etc.)
        /// 4. SettingsManager - Loads user preferences and configuration
        /// 
        /// Each service is designed as a singleton to ensure consistent state across
        /// the application lifecycle and prevent resource conflicts.
        /// </remarks>
        private void InitializeServices()
        {
            // Initialize platform-specific Windows integration services
            // This sets up DPI awareness, Win32 API wrappers, and system integration
            WindowsIntegration.Initialize();
            
            // Hide the Windows taskbar since we're replacing it with SeroDesk
            // This prevents the original taskbar from interfering with our dock
            TaskbarManager.HideTaskbar();
            
            // Start a timer to continuously monitor and hide the taskbar
            // Windows sometimes restores the taskbar, so we need to keep hiding it
            StartTaskbarMonitoring();
            
            // Terminate Explorer.exe and assume shell responsibilities
            // This is the core functionality that makes SeroDesk a true shell replacement
            ExplorerManager.Instance.KillExplorer();
            
            // Start all background services for widget management, notifications, etc.
            // These services run continuously to provide real-time functionality
            ServiceManager.Instance.Initialize();
            
            // Load user settings and preferences from persistent storage
            // This includes widget configurations, theme settings, and user customizations
            SettingsManager.Instance.LoadSettings();
            
            // Create and show the separate status bar window that overlays all applications
            CreateStatusBarWindow();
        }
        
        /// <summary>
        /// Creates and initializes the separate status bar window that can overlay all applications
        /// </summary>
        private void CreateStatusBarWindow()
        {
            try
            {
                var statusBarWindow = new Views.StatusBarWindow();
                statusBarWindow.Show();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to create status bar window: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Starts a timer to continuously monitor and hide the Windows taskbar.
        /// This is necessary because Windows sometimes restores the taskbar automatically.
        /// </summary>
        private void StartTaskbarMonitoring()
        {
            _taskbarHideTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2) // Check every 2 seconds
            };
            _taskbarHideTimer.Tick += (sender, e) =>
            {
                // Force hide the taskbar if it's visible
                if (!TaskbarManager.IsTaskbarHidden)
                {
                    TaskbarManager.ForceHideTaskbar();
                }
                else
                {
                    // Even if we think it's hidden, force hide it again to be sure
                    TaskbarManager.ForceHideTaskbar();
                }
            };
            _taskbarHideTimer.Start();
        }
        
        /// <summary>
        /// Stops the taskbar monitoring timer
        /// </summary>
        private void StopTaskbarMonitoring()
        {
            _taskbarHideTimer?.Stop();
            _taskbarHideTimer = null;
        }
        
        /// <summary>
        /// Handles application shutdown and cleanup operations when SeroDesk exits.
        /// This method ensures graceful restoration of the Windows desktop environment
        /// and proper cleanup of all resources and services.
        /// </summary>
        /// <param name="e">Exit event arguments containing the application exit code</param>
        /// <remarks>
        /// Shutdown sequence ensures system stability:
        /// 1. Restores Explorer.exe if it was initially running (critical for user experience)
        /// 2. Gracefully shuts down all background services to prevent resource leaks
        /// 3. Persists user settings and configurations to storage
        /// 4. Releases the single-instance mutex to allow future SeroDesk instances
        /// 
        /// This cleanup is essential to prevent system instability and ensure users
        /// can return to the standard Windows desktop environment.
        /// </remarks>
        protected override void OnExit(ExitEventArgs e)
        {
            // Stop taskbar monitoring
            StopTaskbarMonitoring();
            
            // Restore the Windows taskbar before exiting
            // This ensures the user has a working taskbar when SeroDesk closes
            TaskbarManager.ShowTaskbar();
            
            // Restore Explorer.exe to return user to standard Windows desktop
            // Only restart if Explorer was initially running to respect user's system state
            if (ExplorerManager.Instance.WasExplorerInitiallyRunning)
            {
                ExplorerManager.Instance.RestartExplorer();
            }
            
            // Gracefully shutdown all background services to prevent resource leaks
            // This includes widget managers, notification services, and system monitors
            ServiceManager.Instance.Shutdown();
            
            // Persist all user settings and configurations to storage
            // Ensures user customizations are preserved for the next session
            SettingsManager.Instance.SaveSettings();
            
            // Release the single-instance mutex to allow future SeroDesk executions
            _mutex?.ReleaseMutex();
            _mutex?.Dispose();
            
            base.OnExit(e);
        }
        
        /// <summary>
        /// Handles unhandled exceptions from any thread in the application domain.
        /// This method provides a last line of defense against application crashes
        /// by logging critical errors and providing user feedback.
        /// </summary>
        /// <param name="sender">The object that raised the unhandled exception event</param>
        /// <param name="e">Event arguments containing the exception details and termination information</param>
        /// <remarks>
        /// This handler catches exceptions that occur outside the UI thread or in
        /// background services. Since these exceptions can be fatal and cause application
        /// termination, comprehensive logging is essential for debugging and user support.
        /// </remarks>
        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            LogException(e.ExceptionObject as Exception, "Unhandled Exception");
        }
        
        /// <summary>
        /// Handles unhandled exceptions from the UI dispatcher thread.
        /// This method catches UI-related exceptions and allows the application
        /// to continue running by marking the exception as handled.
        /// </summary>
        /// <param name="sender">The dispatcher that raised the unhandled exception event</param>
        /// <param name="e">Event arguments containing the exception and handling options</param>
        /// <remarks>
        /// UI thread exceptions are often recoverable, so this handler marks them as handled
        /// to prevent application termination. However, the exception is still logged for
        /// debugging purposes. This approach maintains application stability while preserving
        /// diagnostic information.
        /// </remarks>
        private void OnDispatcherUnhandledException(object sender, 
            System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            LogException(e.Exception, "Dispatcher Unhandled Exception");
            e.Handled = true; // Prevent application termination for UI thread exceptions
        }
        
        /// <summary>
        /// Logs exception details to a file and displays user-friendly error information.
        /// This method creates comprehensive crash logs for debugging and provides
        /// users with actionable information about application errors.
        /// </summary>
        /// <param name="ex">The exception to log, or null if no exception object is available</param>
        /// <param name="source">A descriptive string indicating the source or context of the exception</param>
        /// <remarks>
        /// The logging system:
        /// 1. Creates timestamped log files in the user's local application data folder
        /// 2. Includes full exception details and stack traces for developer debugging
        /// 3. Shows users the log file location for support purposes
        /// 4. Uses user-friendly error messages to avoid technical confusion
        /// 
        /// Log files are stored in %LocalAppData%\SeroDesk\logs\ with timestamps
        /// to allow tracking of multiple crash incidents and debugging patterns.
        /// </remarks>
        private void LogException(Exception? ex, string source)
        {
            if (ex == null) return;
            
            // Create timestamped log file in user's local application data
            var logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SeroDesk", "logs", $"crash_{DateTime.Now:yyyyMMdd_HHmmss}.log");
            
            // Ensure the logs directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
            
            // Write comprehensive exception information for debugging
            File.WriteAllText(logPath, $"{source}: {ex}\n\nStack Trace:\n{ex.StackTrace}");
            
            // Show user-friendly error message with log file location for support
            MessageBox.Show(
                $"An unexpected error occurred. Error details have been saved to:\n{logPath}",
                "SeroDesk Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}