using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using SeroDesk.Models;
using SeroDesk.ViewModels;

namespace SeroDesk.Views
{
    public partial class SeroNotificationCenter : UserControl
    {
        private NotificationCenterViewModel? _viewModel;
        private bool _isVisible = false;
        
        public SeroNotificationCenter()
        {
            InitializeComponent();
            
            Loaded += (s, e) =>
            {
                _viewModel = DataContext as NotificationCenterViewModel;
                if (_viewModel == null)
                {
                    _viewModel = new NotificationCenterViewModel();
                    DataContext = _viewModel;
                }
            };
        }
        
        public void Show()
        {
            if (_isVisible) return;
            
            _isVisible = true;
            NotificationPanel.Visibility = Visibility.Visible;
            
            // Slide down animation
            var slideDown = new DoubleAnimation
            {
                From = -1080,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(400),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            
            NotificationTransform.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, slideDown);
        }
        
        public void Hide()
        {
            if (!_isVisible) return;
            
            _isVisible = false;
            
            // Slide up animation
            var slideUp = new DoubleAnimation
            {
                From = 0,
                To = -1080,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            
            slideUp.Completed += (s, e) =>
            {
                NotificationPanel.Visibility = Visibility.Collapsed;
            };
            
            NotificationTransform.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, slideUp);
        }
        
        public new bool IsVisible => _isVisible;
        
        private void ClearAllNotifications_Click(object sender, RoutedEventArgs e)
        {
            _viewModel?.ClearAllNotifications();
        }
        
        private void DismissNotification_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is NotificationItem notification)
            {
                _viewModel?.RemoveNotification(notification);
            }
        }
    }
}