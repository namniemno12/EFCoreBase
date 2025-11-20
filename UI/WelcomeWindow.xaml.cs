using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace UI
{
    public partial class WelcomeWindow : Window
    {
        private readonly string _username;
        private readonly string _approvedBy;
        private readonly DateTime _approvedTime;

        public WelcomeWindow(string username, string approvedBy, DateTime approvedTime)
        {
            InitializeComponent();

            _username = username;
            _approvedBy = approvedBy;
            _approvedTime = approvedTime;

            LoadUserInfo();
            PlayWelcomeAnimation();
        }

        private void LoadUserInfo()
        {
            UsernameTextBlock.Text = _username;
            ApprovedByTextBlock.Text = _approvedBy;
            ApprovedTimeTextBlock.Text = _approvedTime.ToString("yyyy-MM-dd HH:mm:ss");
        }

        private void PlayWelcomeAnimation()
        {
            // Fade in animation
            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.5),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            this.BeginAnimation(Window.OpacityProperty, fadeIn);
        }

        private void ContinueButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Navigate to main dashboard or application screen
            MessageBox.Show(
                $"Welcome to the system, {_username}!\n\n" +
                "Dashboard functionality will be implemented here.",
                "Dashboard",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to logout?",
                "Confirm Logout",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Fade out animation before closing
                var fadeOut = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(0.3)
                };

                fadeOut.Completed += (s, args) =>
                {
                    // Return to login screen
                    var loginWindow = new MainWindow();
                    loginWindow.Show();
                    this.Close();
                };

                this.BeginAnimation(Window.OpacityProperty, fadeOut);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to exit the application?",
                "Confirm Exit",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                Application.Current.Shutdown();
            }
        }
    }
}
