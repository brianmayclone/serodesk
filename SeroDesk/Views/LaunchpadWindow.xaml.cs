using System.Windows;
using System.Windows.Interop;
using SeroDesk.Platform;

namespace SeroDesk.Views
{
    public partial class LaunchpadWindow : Window
    {
        public LaunchpadWindow()
        {
            InitializeComponent();
            
            // Note: Do NOT initialize the launchpad here - DataContext is set from MainWindow
            // to share the same ViewModel and prevent empty instances
            
            // Ensure this window is ALWAYS on top
            Loaded += LaunchpadWindow_Loaded;
        }
        
        private void LaunchpadWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            
            // Force window to be topmost and steal focus
            WindowsIntegration.SetWindowAlwaysOnTop(hwnd);
        }
        
        public void ShowLaunchpad()
        {
            Visibility = Visibility.Visible;
            Launchpad.Show();
        }
        
        public void HideLaunchpad()
        {
            Launchpad.Hide();
            Visibility = Visibility.Collapsed;
        }
    }
}