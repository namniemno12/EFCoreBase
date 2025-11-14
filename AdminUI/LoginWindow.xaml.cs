using MyProject.Application.Services.Interfaces;
using MyProject.Domain.DTOs.Auth.Req;
using System;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;

namespace AdminUI
{
    public partial class LoginWindow : Window
    {
        private readonly IAuthServices _authServices;

        public LoginWindow()
        {
            InitializeComponent();
        
            // ✅ Get AuthServices from DI Container
            _authServices = App.ServiceProvider.GetRequiredService<IAuthServices>();

            Loaded += (s, e) => UsernameTextBox.Focus();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                LoginButton_Click(sender, e);
            }
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            var username = UsernameTextBox.Text.Trim();
            var password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ShowStatusMessage("Please enter both username and password!", false);
                return;
            }

            LoginButton.IsEnabled = false;
            LoginButton.Content = "LOGGING IN...";
            ShowStatusMessage("Authenticating...", true);

            try
            {
                // ✅ Step 1: Login qua AuthServices
                Console.WriteLine($"🔐 LoginWindow: Authenticating {username}...");

                var request = new LoginCmsRequest
                {
                    UserName = username,
                    Password = password
                };

                var result = await _authServices.LoginByAdmin(request);

                if (result.ResponseCode != 200 || result.Data == null)
                {
                    ShowStatusMessage(result.Message, false);
                    PasswordBox.Clear();
                    PasswordBox.Focus();
                    LoginButton.IsEnabled = true;
                    LoginButton.Content = "LOGIN";
                    return;
                }

                var adminName = username;
                var adminId = result.Data.AdminId ?? Guid.Empty;
                var accessToken = result.Data.AccessToken;
                var refreshToken = result.Data.RefreshToken;

                Console.WriteLine($"✅ LoginWindow: Login successful for {adminName}");
                Console.WriteLine($"✅ LoginWindow: AdminId: {adminId}");

                ShowStatusMessage("Login successful! Connecting to server...", true);

                // ✅ Step 2: Connect TCP
                Console.WriteLine("🔌 LoginWindow: Connecting to TCP Socket Server...");

                var tcpClient = new TcpClient();

                // Set timeout
                var connectTask = tcpClient.ConnectAsync("localhost", 9000);
                var timeoutTask = System.Threading.Tasks.Task.Delay(5000);

                var completedTask = await System.Threading.Tasks.Task.WhenAny(connectTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    throw new Exception("Connection timeout. Please ensure TcpSocketServer is running on port 9000.");
                }

                await connectTask;

                var stream = tcpClient.GetStream();

                Console.WriteLine("✅ LoginWindow: TCP connected successfully");

                // ✅ Step 3: Send AdminConnect message
                Console.WriteLine("📤 LoginWindow: Sending AdminConnect message...");

                var adminConnectMessage = new
                {
                    Method = "AdminConnect",
                    Data = new
                    {
                        AdminId = adminId,
                        AdminName = adminName,
                        AccessToken = accessToken
                    }
                };

                var json = JsonSerializer.Serialize(adminConnectMessage);
                var bytes = Encoding.UTF8.GetBytes(json);
                await stream.WriteAsync(bytes, 0, bytes.Length);

                Console.WriteLine("✅ LoginWindow: AdminConnect sent, waiting for response...");

                // ✅ Step 4: Wait for AdminConnectAck
                var buffer = new byte[8192];
                var readTask = stream.ReadAsync(buffer, 0, buffer.Length);
                var readTimeoutTask = System.Threading.Tasks.Task.Delay(10000);

                var readCompleted = await System.Threading.Tasks.Task.WhenAny(readTask, readTimeoutTask);

                if (readCompleted == readTimeoutTask)
                {
                    throw new Exception("Server did not respond within 10 seconds");
                }

                var bytesRead = await readTask;

                if (bytesRead > 0)
                {
                    var response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine($"📥 LoginWindow: Server response: {response}");

                    var doc = JsonDocument.Parse(response);
                    var method = doc.RootElement.GetProperty("Method").GetString();

                    if (method == "AdminConnectAck")
                    {
                        var data = doc.RootElement.GetProperty("Data");
                        var isSuccess = data.GetProperty("IsSuccess").GetBoolean();

                        if (!isSuccess)
                        {
                            var message = data.GetProperty("Message").GetString();
                            throw new Exception($"Server rejected connection: {message}");
                        }

                        Console.WriteLine("✅ LoginWindow: AdminConnectAck received successfully");
                    }
                    else if (method == "Error")
                    {
                        var errorMsg = doc.RootElement.GetProperty("Data").GetProperty("Message").GetString();
                        throw new Exception($"Server error: {errorMsg}");
                    }
                }

                ShowStatusMessage("Connected! Opening dashboard...", true);

                // ✅ Step 5: Show MessageBox
                MessageBox.Show(
                    $"Welcome, {adminName}!\n\nLogin successful!",
                    "Login Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // ✅ Step 6: Create and show MainWindow
                Console.WriteLine("🏗️ LoginWindow: Creating MainWindow...");

                var mainWindow = new MainWindow(
                    adminName,
                    adminId,
                    tcpClient,
                    stream,
                    accessToken,
                    refreshToken);

                // ✅ ẨN LoginWindow thay vì đóng để giữ TCP connection alive
                this.Hide();
                Console.WriteLine("✅ LoginWindow: Hidden, showing MainWindow...");

                mainWindow.Show();

                Console.WriteLine("✅ LoginWindow: MainWindow shown successfully");

                // ✅ Đóng LoginWindow khi MainWindow đóng
                mainWindow.Closed += (s, args) =>
                {
                    Console.WriteLine("✅ LoginWindow: MainWindow closed, closing LoginWindow...");
                    this.Close();
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ LoginWindow: Exception: {ex.Message}");
                Console.WriteLine($"❌ LoginWindow: StackTrace: {ex.StackTrace}");

                ShowStatusMessage($"Error: {ex.Message}", false);

                MessageBox.Show(
                    $"Failed to connect:\n\n{ex.Message}\n\nPlease ensure TcpSocketServer is running.",
                    "Connection Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                LoginButton.IsEnabled = true;
                LoginButton.Content = "LOGIN";
            }
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
    }
}
