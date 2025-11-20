using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyProject.Domain.Entities;
using MyProject.Infrastructure;

namespace UI
{
    public partial class WelcomeWindow : Window
    {
        private readonly Guid _userId;
        private readonly string _approvedBy;
        private readonly DateTime _approvedTime;

        public WelcomeWindow(Guid userId, string approvedBy, DateTime approvedTime)
        {
            InitializeComponent();

            _userId = userId;
            _approvedBy = approvedBy;
            _approvedTime = approvedTime;

            // Load user info from database
            Loaded += async (s, e) => await LoadUserInfoAsync();
            
            PlayWelcomeAnimation();
        }

        private async Task LoadUserInfoAsync()
        {
            try
            {
                using (var scope = App.ServiceProvider.CreateScope())
                {
                    var userRepository = scope.ServiceProvider.GetRequiredService<IRepositoryAsync<Users>>();
                    
                    var user = await userRepository.AsQueryable()
                        .Where(x => x.Id == _userId)
                        .FirstOrDefaultAsync();

                    if (user != null)
                    {
                        UsernameTextBlock.Text = user.UserName;
                        FullNameTextBlock.Text = string.IsNullOrEmpty(user.FullName) ? "N/A" : user.FullName;
                        EmailTextBlock.Text = string.IsNullOrEmpty(user.Email) ? "N/A" : user.Email;
                        PhoneTextBlock.Text = string.IsNullOrEmpty(user.PhoneNumber) ? "N/A" : user.PhoneNumber;
                        ApprovedByTextBlock.Text = _approvedBy;
                        ApprovedTimeTextBlock.Text = _approvedTime.ToString("yyyy-MM-dd HH:mm:ss");
                    }
                    else
                    {
                        MessageBox.Show("Failed to load user information!", "Error", 
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading user info: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
                $"Welcome to the system, {FullNameTextBlock.Text}!\n\n" +
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
