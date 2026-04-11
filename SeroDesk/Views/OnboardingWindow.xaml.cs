using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace SeroDesk.Views
{
    public partial class OnboardingWindow : Window
    {
        #pragma warning disable CS0414
        private int _currentPage = 0;
        #pragma warning restore CS0414

        public OnboardingWindow()
        {
            InitializeComponent();
        }

        private void NextPage_Click(object sender, RoutedEventArgs e)
        {
            _currentPage = 1;

            // Fade out page 1
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(200));
            fadeOut.Completed += (s, args) =>
            {
                Page1.Visibility = Visibility.Collapsed;
                Page2.Visibility = Visibility.Visible;
                Page2.Opacity = 0;

                var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                Page2.BeginAnimation(OpacityProperty, fadeIn);
            };
            Page1.BeginAnimation(OpacityProperty, fadeOut);

            // Update dots
            Dot1.Fill = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromArgb(0x50, 0xFF, 0xFF, 0xFF));
            Dot2.Fill = System.Windows.Media.Brushes.White;
        }

        private void Finish_Click(object sender, RoutedEventArgs e)
        {
            // Mark onboarding as complete
            Services.OnboardingService.MarkComplete();

            // Fade out the entire window
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(400));
            fadeOut.Completed += (s, args) => Close();
            BeginAnimation(OpacityProperty, fadeOut);
        }
    }
}
