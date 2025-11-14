using MyProject.Application.Services.Interfaces;
using MyProject.Domain.DTOs.Auth.Req;
using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;

namespace UI
{
    public partial class RegisterWindow : Window
    {
  private readonly IAuthServices _authServices;

        public RegisterWindow()
        {
   InitializeComponent();

         // ? Get AuthServices from DI Container
          _authServices = App.ServiceProvider.GetRequiredService<IAuthServices>();

            Loaded += (s, e) => UsernameTextBox.Focus();
        }

     private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            var loginWindow = new MainWindow();
     loginWindow.Show();
 this.Close();
  }

        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
     var username = UsernameTextBox.Text.Trim();
      var email = EmailTextBox.Text.Trim();
   var password = PasswordBox.Password;
            var fullName = FullNameTextBox.Text.Trim();
      var phoneNumber = PhoneNumberTextBox.Text.Trim();
         var address = AddressTextBox.Text.Trim();

            // Validation
   if (string.IsNullOrWhiteSpace(username))
            {
      ShowStatusMessage("Username is required!", false);
            UsernameTextBox.Focus();
   return;
         }

   if (string.IsNullOrWhiteSpace(email))
            {
        ShowStatusMessage("Email is required!", false);
    EmailTextBox.Focus();
         return;
            }

            if (!IsValidEmail(email))
     {
       ShowStatusMessage("Invalid email format!", false);
 EmailTextBox.Focus();
    return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
          ShowStatusMessage("Password is required!", false);
          PasswordBox.Focus();
 return;
            }

            if (password.Length < 6)
     {
   ShowStatusMessage("Password must be at least 6 characters!", false);
 PasswordBox.Focus();
                return;
      }

      RegisterButton.IsEnabled = false;
            RegisterButton.Content = "REGISTERING...";
            ShowStatusMessage("Creating your account...", true);

          try
   {
           Console.WriteLine($"?? RegisterWindow: Registering user {username}...");

  var request = new RegisterReq
            {
        UserName = username,
       Email = email,
                Password = password,
     FullName = string.IsNullOrWhiteSpace(fullName) ? null : fullName,
        PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber,
 Address = string.IsNullOrWhiteSpace(address) ? null : address
           };

        var result = await _authServices.Register(request);

   if (result.ResponseCode != 200)
           {
  ShowStatusMessage(result.Message, false);
          Console.WriteLine($"? RegisterWindow: Registration failed: {result.Message}");
   RegisterButton.IsEnabled = true;
    RegisterButton.Content = "REGISTER";
           return;
 }

         Console.WriteLine($"? RegisterWindow: Registration successful for {username}");
                ShowStatusMessage("Registration successful! Redirecting to login...", true);

                // Show success message
                MessageBox.Show(
      $"Welcome, {username}!\n\nYour account has been created successfully.\nPlease login to continue.",
      "Registration Successful",
          MessageBoxButton.OK,
        MessageBoxImage.Information);

         // Redirect to Login Window
 var loginWindow = new MainWindow();
      loginWindow.Show();
    this.Close();
       }
          catch (Exception ex)
          {
          ShowStatusMessage($"Error: {ex.Message}", false);
 Console.WriteLine($"? RegisterWindow: Exception: {ex.Message}");
                Console.WriteLine($"? RegisterWindow: StackTrace: {ex.StackTrace}");

         RegisterButton.IsEnabled = true;
    RegisterButton.Content = "REGISTER";
    }
        }

      private void BackToLogin_Click(object sender, RoutedEventArgs e)
     {
 var loginWindow = new MainWindow();
     loginWindow.Show();
     this.Close();
  }

        private void ShowStatusMessage(string message, bool isLoading)
        {
   StatusBorder.Visibility = Visibility.Visible;
    StatusTextBlock.Text = message;

            if (isLoading)
          {
       StatusTextBlock.Foreground = new SolidColorBrush(Colors.White);
       }
   else if (message.Contains("success", StringComparison.OrdinalIgnoreCase))
       {
        StatusTextBlock.Foreground = new SolidColorBrush(Colors.LightGreen);
          }
            else
     {
            StatusTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(255, 200, 200));
  }
        }

        private bool IsValidEmail(string email)
        {
  try
      {
         var addr = new System.Net.Mail.MailAddress(email);
          return addr.Address == email;
  }
          catch
      {
                return false;
  }
        }
    }
}
