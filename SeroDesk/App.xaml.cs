using System.IO;
using System.Windows;
using SeroDesk.Services;
using SeroDesk.Platform;

namespace SeroDesk
{
    public partial class App : Application
    {
        private static Mutex? _mutex;
        
        protected override void OnStartup(StartupEventArgs e)
        {
            // Ensure single instance
            const string appName = "SeroDesk-{8F6F0AC4-B9A1-45fd-A8CF-72F04E6BDE8F}";
            _mutex = new Mutex(true, appName, out bool createdNew);
            
            if (!createdNew)
            {
                MessageBox.Show("SeroDesk is already running.", "SeroDesk", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                Shutdown();
                return;
            }
            
            // Setup unhandled exception handling
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            
            // Initialize services
            InitializeServices();
            
            base.OnStartup(e);
        }
        
        private void InitializeServices()
        {
            // Initialize platform services
            WindowsIntegration.Initialize();
            
            // Kill explorer for true shell replacement
            ExplorerManager.Instance.KillExplorer();
            
            // Start background services
            ServiceManager.Instance.Initialize();
            
            // Load user settings
            SettingsManager.Instance.LoadSettings();
        }
        
        protected override void OnExit(ExitEventArgs e)
        {
            // Restart explorer if it was running initially
            if (ExplorerManager.Instance.WasExplorerInitiallyRunning)
            {
                ExplorerManager.Instance.RestartExplorer();
            }
            
            // Cleanup services
            ServiceManager.Instance.Shutdown();
            
            // Save settings
            SettingsManager.Instance.SaveSettings();
            
            // Release mutex
            _mutex?.ReleaseMutex();
            _mutex?.Dispose();
            
            base.OnExit(e);
        }
        
        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            LogException(e.ExceptionObject as Exception, "Unhandled Exception");
        }
        
        private void OnDispatcherUnhandledException(object sender, 
            System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            LogException(e.Exception, "Dispatcher Unhandled Exception");
            e.Handled = true;
        }
        
        private void LogException(Exception? ex, string source)
        {
            if (ex == null) return;
            
            // Log to file
            var logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SeroDesk", "logs", $"crash_{DateTime.Now:yyyyMMdd_HHmmss}.log");
            
            Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
            
            File.WriteAllText(logPath, $"{source}: {ex}\n\nStack Trace:\n{ex.StackTrace}");
            
            // Show user-friendly error
            MessageBox.Show(
                $"An unexpected error occurred. Error details have been saved to:\n{logPath}",
                "SeroDesk Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}