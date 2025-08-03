using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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
        
        /// <summary>
        /// Sets the background transparency of the status bar
        /// </summary>
        /// <param name="isTransparent">True for transparent (desktop), false for opaque (overlay)</param>
        public void SetBackgroundTransparency(bool isTransparent)
        {
            if (StatusBarBackground != null)
            {
                if (isTransparent)
                {
                    // Completely transparent for desktop/launchboard
                    StatusBarBackground.Background = new SolidColorBrush(Color.FromArgb(0x00, 0x00, 0x00, 0x00));
                }
                else
                {
                    // Semi-opaque dark background for overlay over applications
                    StatusBarBackground.Background = new SolidColorBrush(Color.FromArgb(0xC0, 0x00, 0x00, 0x00));
                }
            }
        }
    }
}