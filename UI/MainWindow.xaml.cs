using System;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration; // ✅ Add this

namespace UI
{
    public partial class MainWindow : Window
    {
        private TcpClient? _client;
        private NetworkStream? _stream;
        private bool _isRunning;

        public MainWindow()
        {
            InitializeComponent();
            
            Loaded += MainWindow_Loaded;
            Closed += MainWindow_Closed;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await ConnectToServerAsync();
        }

        private async Task ConnectToServerAsync()
        {
            try
            {
                LogMessage("🔄 Connecting to server...");
                
                // ✅ Đọc server host từ configuration
                var configuration = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions
   .GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>(App.ServiceProvider);
               var serverHost = configuration.GetValue<string>("TcpServer:Host", "localhost");
   var serverPort = configuration.GetValue<int>("TcpServer:Port", 9000);

                LogMessage($"📡 Target server: {serverHost}:{serverPort}");

                _client = new TcpClient();
                await _client.ConnectAsync(serverHost, serverPort);
                _stream = _client.GetStream();
                _isRunning = true;

                UpdateConnectionStatus(true);
                LogMessage("✅ Connected to TCP Server successfully!");
                LoginButton.IsEnabled = true;

                // Start listening for messages
                _ = Task.Run(ListenForMessagesAsync);
            }
            catch (Exception ex)
            {
                UpdateConnectionStatus(false);
                LogMessage($"❌ Connection failed: {ex.Message}");
                LoginButton.IsEnabled = false;
            }
        }

        private async Task ListenForMessagesAsync()
        {
            var buffer = new byte[8192];

            while (_isRunning && _client?.Connected == true)
            {
                try
                {
                    if (_stream == null) break;

                    int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead <= 0) break;

                    var json = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            var doc = JsonDocument.Parse(json);
                            var method = doc.RootElement.GetProperty("Method").GetString();

                            switch (method)
                            {
                                case "LoginRequestAck":
                                    HandleLoginRequestAck(doc.RootElement);
                                    break;

                                case "LoginResult":
                                    HandleLoginResult(doc.RootElement);
                                    break;

                                case "Error":
                                    HandleError(doc.RootElement);
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            LogMessage($"⚠️ Error parsing message: {ex.Message}");
                        }
                    });
                }
                catch (Exception ex)
                {
                    if (_isRunning)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            LogMessage($"⚠️ Connection error: {ex.Message}");
                            UpdateConnectionStatus(false);
                        });
                    }
                    break;
                }
            }
        }

        private void HandleLoginRequestAck(JsonElement root)
        {
            var data = root.GetProperty("Data");
            var isSuccess = data.GetProperty("IsSuccess").GetBoolean();
            var message = data.GetProperty("Message").GetString();

            if (isSuccess)
            {
                // Account validated successfully
                var loginRequestId = data.GetProperty("LoginRequestId").GetString();

                LogMessage($"\n{message}");
                LogMessage($"📝 Request ID: {loginRequestId}");
                LogMessage("⏳ Waiting for admin approval...\n");

                MessageBox.Show("Đã gửi yêu cầu xác thực!", "Login Request", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                // Account validation failed
                LogMessage($"\n❌ ═══════════════════════════");
                LogMessage("❌   LOGIN FAILED   ❌");
                LogMessage("❌ ═══════════════════════════");
                LogMessage($"{message}");
                LogMessage("═══════════════════════════\n");
                string errorMsg =
          "❌ LOGIN FAILED ❌\n\n" +
          $"{message}";

                MessageBox.Show(errorMsg, "Login Failed", MessageBoxButton.OK, MessageBoxImage.Error);

                // Re-enable login button để user có thể thử lại
                Dispatcher.Invoke(() => LoginButton.IsEnabled = true);
            }
        }

        private void HandleLoginResult(JsonElement root)
        {
            var data = root.GetProperty("Data");
            var isSuccess = data.GetProperty("IsSuccess").GetBoolean();
            var message = data.GetProperty("Message").GetString();

            if (isSuccess)
            {
                // ✅ LOGIN APPROVED - Navigate to Welcome Window
                LogMessage("\n🎉 ═══════════════════════════");
                LogMessage("🎉   LOGIN APPROVED!   🎉");
                LogMessage("🎉 ═══════════════════════════");
                LogMessage($"✅ {message}");

                string approvedBy = "Admin";
                DateTime approvedTime = DateTime.Now;
                Guid userId = Guid.Empty;

                if (data.TryGetProperty("ApprovedBy", out var approvedByProperty))
                {
                    approvedBy = approvedByProperty.GetString() ?? "Admin";
                    LogMessage($"👔 Approved by: {approvedBy}");
                }

                if (data.TryGetProperty("ApprovedAt", out var approvedAtProperty))
                {
                    approvedTime = approvedAtProperty.GetDateTime();
                    LogMessage($"🕐 Time: {approvedTime:yyyy-MM-dd HH:mm:ss}");
                }

                // ✅ Extract UserId from response
                if (data.TryGetProperty("UserId", out var userIdProperty))
                {
                    if (Guid.TryParse(userIdProperty.GetString(), out var parsedUserId))
                    {
                        userId = parsedUserId;
                        LogMessage($"👤 User ID: {userId}");
                    }
                }

                if (userId == Guid.Empty)
                {
                    LogMessage("❌ Failed to get User ID from server response");
                    MessageBox.Show("Error: Invalid user ID received from server", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                LogMessage("═══════════════════════════\n");

                Dispatcher.Invoke(() =>
                {
                    // Show welcome window with UserId - it will load user info from database
                    var welcomeWindow = new WelcomeWindow(
                        userId,
                        approvedBy,
                        approvedTime
                    );
                    
                    welcomeWindow.Show();
                    
                    // Close current login window
                    this.Close();
                });
            }
            else
            {
                // ❌ LOGIN REJECTED - Show MessageBox only
                string msg =
                    "❌ LOGIN REJECTED ❌\n\n" +
                    $"⚠️ {message}\n";

                LogMessage("\n❌ ═══════════════════════════");
                LogMessage("❌   LOGIN REJECTED   ❌");
                LogMessage("❌ ═══════════════════════════");
                LogMessage($"⚠️ {message}");

                string rejectedBy = "Admin";
                if (data.TryGetProperty("RejectedBy", out var rejectedByProperty))
                {
                    rejectedBy = rejectedByProperty.GetString() ?? "Admin";
                    LogMessage($"👔 Rejected by: {rejectedBy}");
                    msg += $"👔 Rejected by: {rejectedBy}\n";
                }

                LogMessage("═══════════════════════════\n");

                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(msg, "Login Rejected", MessageBoxButton.OK, MessageBoxImage.Warning);
                    
                    LoginButton.IsEnabled = true;
                    PasswordBox.Clear();
                    PasswordBox.Focus();
                });
            }
        }

        private void HandleError(JsonElement root)
        {
            var data = root.GetProperty("Data");
            var errorMessage = data.GetProperty("Message").GetString();
            LogMessage($"❌ Server Error: {errorMessage}\n");
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(UsernameTextBox.Text))
            {
                MessageBox.Show("Please enter a username!", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(PasswordBox.Password))
            {
                MessageBox.Show("Please enter a password!", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_stream == null || !_client.Connected)
            {
                MessageBox.Show("Not connected to server!", "Connection Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                LoginButton.IsEnabled = false;

                var message = new
                {
                    Method = "LoginRequest",
                    Data = new
                    {
                        UserName = UsernameTextBox.Text,
                        Password = PasswordBox.Password,
                        IpAddress = "127.0.0.1",
                        DeviceInfo = "Windows WPF Client"
                    }
                };

                var json = JsonSerializer.Serialize(message);
                var bytes = Encoding.UTF8.GetBytes(json);
                await _stream.WriteAsync(bytes, 0, bytes.Length);

                LogMessage($"📤 Login request sent for user: {UsernameTextBox.Text}");
                LogMessage("🔐 Validating credentials...");
            }
            catch (Exception ex)
            {
                LogMessage($"❌ Error sending login request: {ex.Message}");
                LoginButton.IsEnabled = true;
            }
        }

        private void UpdateConnectionStatus(bool isConnected)
        {
            Dispatcher.Invoke(() =>
            {
                if (isConnected)
                {
                    StatusIndicator.Fill = new SolidColorBrush(Colors.LimeGreen);
                    StatusText.Text = "Connected";
                    StatusText.Foreground = new SolidColorBrush(Colors.Green);
                    StatusText.FontWeight = FontWeights.Bold;
                }
                else
                {
                    StatusIndicator.Fill = new SolidColorBrush(Colors.Red);
                    StatusText.Text = "Disconnected";
                    StatusText.Foreground = new SolidColorBrush(Colors.Red);
                    StatusText.FontWeight = FontWeights.Normal;
                    LoginButton.IsEnabled = false;
                }
            });
        }

        private void LogMessage(string message)
        {
            Dispatcher.Invoke(() =>
            {
                var timestamp = DateTime.Now.ToString("HH:mm:ss");
                LogTextBlock.Text += $"[{timestamp}] {message}\n";
            });
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            _isRunning = false;
            _stream?.Close();
            _client?.Close();
        }

        private void RegisterLink_Click(object sender, RoutedEventArgs e)
        {
            var registerWindow = new RegisterWindow();
            registerWindow.Show();
            this.Close();
        }
    }
}