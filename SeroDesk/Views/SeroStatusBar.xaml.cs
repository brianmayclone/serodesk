using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SeroDesk.Services;

namespace SeroDesk.Views
{
    public partial class SeroStatusBar : UserControl
    {
        public event EventHandler? LeftSideClicked;
        public event EventHandler? RightSideClicked;
        
        public SeroStatusBar()
        {
            InitializeComponent();
            DataContext = SystemStatusService.Instance;
            
            // Show debug button only in debug builds
#if DEBUG
            DebugExitButton.Visibility = Visibility.Visible;
#else
            DebugExitButton.Visibility = Visibility.Collapsed;
#endif
            
            // Add click handlers for left and right sides
            this.MouseLeftButtonUp += SeroStatusBar_MouseLeftButtonUp;
        }
        
        private void SeroStatusBar_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var clickX = e.GetPosition(this).X;
            var halfWidth = this.ActualWidth / 2;
            
            if (clickX < halfWidth)
            {
                // Left side clicked - show notification center
                LeftSideClicked?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                // Right side clicked - show control center  
                RightSideClicked?.Invoke(this, EventArgs.Empty);
            }
        }
        
        private void DebugExitButton_Click(object sender, RoutedEventArgs e)
        {
            // Confirm exit
            var result = MessageBox.Show(
                "Exit SeroDesk and restart Windows Explorer?", 
                "Debug Exit", 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Question);
                
            if (result == MessageBoxResult.Yes)
            {
                // Restart explorer first, then exit
                ExplorerManager.Instance.RestartExplorer();
                
                // Wait a moment for explorer to start
                System.Threading.Thread.Sleep(2000);
                
                // Exit application
                Application.Current.Shutdown();
            }
        }
    }
}