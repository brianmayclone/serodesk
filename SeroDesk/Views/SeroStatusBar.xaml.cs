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
            
            // Keep the visual status bar clean; debug exit stays on Shift+Esc.
#if DEBUG
            var showDebugExit = !App.IsDebugCaptureMode && System.Diagnostics.Debugger.IsAttached;
            DebugExitButton.Visibility = showDebugExit ? Visibility.Visible : Visibility.Collapsed;
            DebugExitContainer.Visibility = showDebugExit ? Visibility.Visible : Visibility.Collapsed;
#else
            DebugExitButton.Visibility = Visibility.Collapsed;
            DebugExitContainer.Visibility = Visibility.Collapsed;
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
                    // Keep a subtle full-width strip so the status area reads like SpringBoard.
                    StatusBarBackground.Background = new LinearGradientBrush(
                        Color.FromArgb(0x56, 0x3E, 0x37, 0x2E),
                        Color.FromArgb(0x18, 0x3E, 0x37, 0x2E),
                        new Point(0, 0),
                        new Point(0, 1));
                }
                else
                {
                    // Stronger overlay when shown above foreground apps.
                    StatusBarBackground.Background = new LinearGradientBrush(
                        Color.FromArgb(0xD8, 0x10, 0x12, 0x16),
                        Color.FromArgb(0xB6, 0x10, 0x12, 0x16),
                        new Point(0, 0),
                        new Point(0, 1));
                }
            }
        }
    }
}
