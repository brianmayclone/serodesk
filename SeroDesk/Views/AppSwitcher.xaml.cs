using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using SeroDesk.Platform;

namespace SeroDesk.Views
{
    public partial class AppSwitcher : UserControl
    {
        private bool _isVisible = false;

        public AppSwitcher()
        {
            InitializeComponent();
        }

        public void Toggle()
        {
            if (_isVisible) Hide();
            else Show();
        }

        public void Show()
        {
            if (_isVisible) return;
            _isVisible = true;

            // Populate with running windows
            AppList.ItemsSource = WindowManager.Instance.Windows;

            SwitcherPanel.Visibility = Visibility.Visible;

            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            SwitcherPanel.BeginAnimation(OpacityProperty, fadeIn);
        }

        public void Hide()
        {
            if (!_isVisible) return;
            _isVisible = false;

            var fadeOut = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(150)
            };
            fadeOut.Completed += (s, e) =>
            {
                SwitcherPanel.Visibility = Visibility.Collapsed;
                AppList.ItemsSource = null;
            };
            SwitcherPanel.BeginAnimation(OpacityProperty, fadeOut);
        }

        public new bool IsVisible => _isVisible;

        private void AppItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is WindowInfo window)
            {
                try
                {
                    if (window.IsMinimized)
                        NativeMethods.ShowWindow(window.Handle, NativeMethods.SW_RESTORE);
                    NativeMethods.SetForegroundWindow(window.Handle);
                }
                catch { }

                Hide();
            }
        }
    }
}
