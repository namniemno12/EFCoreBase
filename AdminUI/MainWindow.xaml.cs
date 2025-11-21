using MyProject.Application.Services.Interfaces;
using MyProject.Domain.DTOs.Auth.Req;
using MyProject.Domain.Entities;
using MyProject.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Messages;
using ToastNotifications.Position;

namespace AdminUI
{
    public partial class MainWindow : Window
    {
        private TcpClient? _client;
        private NetworkStream? _stream;
        private bool _isRunning;
        private Guid _adminId;
        private string _adminName = string.Empty;
        private string _accessToken = string.Empty;
        private string _refreshToken = string.Empty;
        private ObservableCollection<LoginRequestItem> _loginRequests = new();
        private ObservableCollection<LoginHistoryItem> _loginHistory = new();
        private ObservableCollection<UserManagementItem> _users = new();
        private ObservableCollection<UserManagementItem> _filteredUsers = new();
        private readonly IAuthServices _authServices;
        private Notifier? _notifier;

        // Pagination for Login History
        private int _currentPage = 1;
        private int _recordPerPage = 10;
        private int _totalPages = 1;

        // Pagination for Users
        private int _usersCurrentPage = 1;
        private int _usersRecordPerPage = 10;
        private int _usersTotalPages = 1;

        // ✅ NEW: Constructor nhận connection đã authenticated và tokens từ LoginWindow
        public MainWindow(string adminName, Guid adminId, TcpClient client, NetworkStream stream,
        string accessToken, string refreshToken, IAuthServices authServices)
        {
            Console.WriteLine("🏗️ MainWindow: Constructor started");

            InitializeComponent();

            Console.WriteLine("✅ MainWindow: InitializeComponent completed");

            _adminName = adminName;
            _adminId = adminId;
            _client = client;
            _stream = stream;
            _accessToken = accessToken;
            _refreshToken = refreshToken;
            _isRunning = true;
            _authServices = authServices;

            _loginRequests = new ObservableCollection<LoginRequestItem>();
            _loginHistory = new ObservableCollection<LoginHistoryItem>();
            _users = new ObservableCollection<UserManagementItem>();
            _filteredUsers = new ObservableCollection<UserManagementItem>();

            // ✅ Bind to ItemsControl (simplest control)
            LoginRequestsItemsControl.ItemsSource = _loginRequests;
            LoginHistoryDataGrid.ItemsSource = _loginHistory;
            UsersDataGrid.ItemsSource = _filteredUsers;

            Console.WriteLine("✅ MainWindow: ItemsControl binding enabled");

            // ✅ Initialize Toast Notifier in Loaded event instead of constructor
            this.Loaded += MainWindow_Loaded;

            // Update UI
            AdminIdTextBlock.Text = _adminId == Guid.Empty ? "Waiting for server..." : _adminId.ToString("N").Substring(0, 16) + "...";
            AdminNameTextBox.Text = _adminName;
            AdminNameTextBox.IsReadOnly = true;

            UpdateConnectionStatus(true);
            LogActivity($"✅ Logged in as: {_adminName}");
            LogActivity($"🔑 Token: {_accessToken[..20]}...");

            Console.WriteLine("✅ MainWindow: Constructor completed");

            // ✅ IMPORTANT: Start TCP listener AFTER window is loaded
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Console.WriteLine("🏗️ MainWindow: Loaded event triggered");

                // ✅ TEMPORARY: Disable Notifier to debug StaticResource error
                /*
                  _notifier = new Notifier(cfg =>
               {
                  cfg.PositionProvider = new WindowPositionProvider(
            parentWindow: this,
              corner: Corner.TopRight,
     offsetX: 10,
        offsetY: 10);

                cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(
         notificationLifetime: TimeSpan.FromSeconds(3),
       maximumNotificationCount: MaximumNotificationCount.FromCount(3));

                  cfg.Dispatcher = Application.Current.Dispatcher;
                   });
                */
              

                Console.WriteLine("⚠️ MainWindow: Notifier disabled for debugging");

                // ✅ Start listening for messages AFTER window is fully loaded
                Console.WriteLine("🚀 MainWindow: Starting TCP listener...");
                _ = Task.Run(ListenForMessagesAsync);
                Console.WriteLine("✅ MainWindow: TCP listener started");

                // ✅ Request pending login requests from server
                _ = Task.Run(async () =>
                {
                    await Task.Delay(500); // Đợi listener ready
                    await RequestPendingLoginRequestsAsync();
                });

                // ✅ Load login history from database
                _ = Task.Run(async () =>
                {
                    await Task.Delay(1000);
                    await LoadLoginHistoryAsync();
                });

                // ✅ Load users list
                _ = Task.Run(async () =>
                {
                    await Task.Delay(1500);
                    await LoadUsersAsync();
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ MainWindow: Failed to initialize: {ex.Message}");
                Console.WriteLine($"❌ MainWindow: StackTrace: {ex.StackTrace}");
            }
        }

        private async Task RequestPendingLoginRequestsAsync()
        {
            try
            {
                Console.WriteLine("📤 MainWindow: Requesting pending login requests...");

                var message = new
                {
                    Method = "GetPendingRequests",
                    Data = new { }
                };

                var json = JsonSerializer.Serialize(message);
                var bytes = Encoding.UTF8.GetBytes(json);
                await _stream!.WriteAsync(bytes, 0, bytes.Length);

                Console.WriteLine("✅ MainWindow: GetPendingRequests sent successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ MainWindow: Failed to request pending requests: {ex.Message}");
                Dispatcher.Invoke(() =>
                {
                    LogActivity($"❌ Failed to get pending requests: {ex.Message}");
                });
            }
        }

        private async Task LoadLoginHistoryAsync()
        {
            try
            {
                Console.WriteLine("📤 MainWindow: Loading login history from database...");

                var result = await _authServices.GetLoginHistory(_currentPage, _recordPerPage);

                if (result.ResponseCode == 200 && result.Data != null)
                {
                    Dispatcher.Invoke(() =>
                    {
                        _loginHistory.Clear();
                        foreach (var item in result.Data)
                        {
                            _loginHistory.Add(new LoginHistoryItem
                            {
                                LoginHistoryId = item.LoginHistoryId,
                                UserId = item.UserId,
                                UserName = item.UserName ?? "",
                                FullName = item.FullName ?? "",
                                IpAddress = item.IpAddress ?? "",
                                DeviceInfo = item.DeviceInfo ?? "",
                                LoginTime = item.LoginTime,
                                IsSuccessful = item.IsSuccessful
                            });
                        }

                        // Update pagination
                        _totalPages = (int)Math.Ceiling((double)result.TotalRecord / _recordPerPage);
                        UpdatePaginationUI();

                        LogActivity($"✅ Loaded {result.Data.Count} login history records (Page {_currentPage}/{_totalPages})");
                    });
                }
                else
                {
                    Dispatcher.Invoke(() =>
                    {
                        LogActivity($"⚠️ Failed to load login history: {result.Message}");
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ MainWindow: Failed to load login history: {ex.Message}");
                Dispatcher.Invoke(() =>
                {
                    LogActivity($"❌ Error loading history: {ex.Message}");
                });
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

                    Console.WriteLine($"📥 MainWindow: Received message: {json}");

                    Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            Console.WriteLine("📝 MainWindow: Parsing message...");

                            var doc = JsonDocument.Parse(json);
                            var method = doc.RootElement.GetProperty("Method").GetString();

                            Console.WriteLine($"📝 MainWindow: Method = {method}");

                            switch (method)
                            {
                                case "PendingLoginRequests":
                                    Console.WriteLine("📝 MainWindow: Handling PendingLoginRequests...");
                                    HandlePendingLoginRequests(doc.RootElement);
                                    break;

                                case "NewLoginRequest":
                                    Console.WriteLine("📝 MainWindow: Handling NewLoginRequest...");
                                    HandleNewLoginRequest(doc.RootElement);
                                    break;

                                case "AcceptLoginAck":
                                    Console.WriteLine("📝 MainWindow: Handling AcceptLoginAck...");
                                    HandleAcceptLoginAck(doc.RootElement);
                                    break;

                                case "LoginHistory":
                                    Console.WriteLine("📝 MainWindow: Handling LoginHistory...");
                                    HandleLoginHistory(doc.RootElement);
                                    break;

                                case "Error":
                                    Console.WriteLine("📝 MainWindow: Handling Error...");
                                    HandleError(doc.RootElement);
                                    break;

                                default:
                                    Console.WriteLine($"⚠️ MainWindow: Unknown method: {method}");
                                    break;
                            }

                            Console.WriteLine("✅ MainWindow: Message handled successfully");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"❌ MainWindow: Error parsing/handling message!");
                            Console.WriteLine($"❌ MainWindow: Exception Type: {ex.GetType().Name}");
                            Console.WriteLine($"❌ MainWindow: Exception Message: {ex.Message}");
                            Console.WriteLine($"❌ MainWindow: StackTrace: {ex.StackTrace}");

                            if (ex.InnerException != null)
                            {
                                Console.WriteLine($"❌ MainWindow: Inner Exception: {ex.InnerException.Message}");
                                Console.WriteLine($"❌ MainWindow: Inner StackTrace: {ex.InnerException.StackTrace}");
                            }

                            LogActivity($"⚠️ Error parsing message: {ex.Message}");
                            _notifier?.ShowError($"❌ Error: {ex.Message}");
                        }
                    });
                }
                catch (Exception ex)
                {
                    if (_isRunning)
                    {
                        Console.WriteLine($"❌ MainWindow: Connection error: {ex.Message}");

                        Dispatcher.Invoke(() =>
                        {
                            LogActivity($"⚠️ Connection error: {ex.Message}");
                            _notifier?.ShowError($"❌ Connection lost: {ex.Message}");
                            UpdateConnectionStatus(false);
                        });
                    }
                    break;
                }
            }

            Console.WriteLine("⛔ MainWindow: ListenForMessagesAsync ended");
        }

        private void HandlePendingLoginRequests(JsonElement root)
        {
            try
            {
                Console.WriteLine("📋 MainWindow: HandlePendingLoginRequests started");

                var data = root.GetProperty("Data");
                var count = data.GetProperty("Count").GetInt32();
                var requests = data.GetProperty("Requests");

                Console.WriteLine($"📋 MainWindow: Count = {count}");

                _loginRequests.Clear();

                foreach (var req in requests.EnumerateArray())
                {
                    Console.WriteLine($"📋 MainWindow: Processing request...");

                    var item = new LoginRequestItem
                    {
                        LoginRequestId = Guid.Parse(req.GetProperty("LoginRequestId").GetString()!),
                        UserId = Guid.Parse(req.GetProperty("UserId").GetString()!),
                        UserName = req.GetProperty("UserName").GetString() ?? "",
                        IpAddress = req.GetProperty("IpAddress").GetString() ?? "N/A",
                        DeviceInfo = req.GetProperty("DeviceInfo").GetString() ?? "N/A",
                        RequestedAt = req.GetProperty("RequestedAt").GetDateTime(),
                        Status = req.GetProperty("Status").GetInt32()
                    };

                    _loginRequests.Add(item);
                    Console.WriteLine($"📋 MainWindow: Added request for {item.UserName}");
                }

                UpdatePendingCount();
                LogActivity($"📋 Loaded {count} pending request(s)");
                _notifier?.ShowInformation($"📋 {count} login requests pending");

                Console.WriteLine("✅ MainWindow: HandlePendingLoginRequests completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ MainWindow: Error in HandlePendingLoginRequests: {ex.Message}");
            }
        }

        private void HandleNewLoginRequest(JsonElement root)
        {
            var data = root.GetProperty("Data");

            var item = new LoginRequestItem
            {
                LoginRequestId = Guid.Parse(data.GetProperty("LoginRequestId").GetString()!),
                UserId = Guid.Parse(data.GetProperty("UserId").GetString()!),
                UserName = data.GetProperty("UserName").GetString() ?? "",
                IpAddress = data.GetProperty("IpAddress").GetString() ?? "N/A",
                DeviceInfo = data.GetProperty("DeviceInfo").GetString() ?? "N/A",
                RequestedAt = data.GetProperty("RequestedAt").GetDateTime(),
                Status = data.GetProperty("Status").GetInt32()
            };

            // Add to top of list
            _loginRequests.Insert(0, item);
            UpdatePendingCount();

            LogActivity($"🔔 New login request from: {item.UserName}");
            _notifier?.ShowInformation($"🔔 New login from {item.UserName}");

            // Visual/Audio notification
            System.Media.SystemSounds.Beep.Play();
        }

        private void HandleLoginHistory(JsonElement root)
        {
            try
            {
                var data = root.GetProperty("Data");

                var item = new LoginHistoryItem
                {
                    LoginHistoryId = Guid.Parse(data.GetProperty("LoginHistoryId").GetString()!),
                    UserId = Guid.Parse(data.GetProperty("UserId").GetString()!),
                    UserName = data.GetProperty("UserName").GetString() ?? "",
                    FullName = data.GetProperty("FullName").GetString() ?? "",
                    IpAddress = data.GetProperty("IpAddress").GetString() ?? "",
                    DeviceInfo = data.GetProperty("DeviceInfo").GetString() ?? "",
                    LoginTime = data.GetProperty("LoginTime").GetDateTime(),
                    IsSuccessful = data.GetProperty("IsSuccessful").GetBoolean()
                };

                // Add to top of history list (real-time notification)
                _loginHistory.Insert(0, item);

                LogActivity($"🕑 {item.UserName} logged in | IP: {item.IpAddress} | Device: {item.DeviceInfo}");
                _notifier?.ShowInformation($"🕑 {item.UserName} logged in");

                System.Media.SystemSounds.Beep.Play();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ MainWindow: Error in HandleLoginHistory: {ex.Message}");
            }
        }
        
        private void HandleAcceptLoginAck(JsonElement root)
        {
            var data = root.GetProperty("Data");
            var message = data.GetProperty("Message").GetString();
            var loginRequestId = Guid.Parse(data.GetProperty("LoginRequestId").GetString()!);

            // Remove from list
            var item = _loginRequests.FirstOrDefault(x => x.LoginRequestId == loginRequestId);
            if (item != null)
            {
                _loginRequests.Remove(item);
                UpdatePendingCount();
            }

            LogActivity($"✅ {message}");
            _notifier?.ShowSuccess($"✅ {message}");
        }
       
            
        private void HandleError(JsonElement root)
        {
            var data = root.GetProperty("Data");
            var errorMessage = data.GetProperty("Message").GetString();
            LogActivity($"❌ Server Error: {errorMessage}");
            _notifier?.ShowError($"❌ {errorMessage}");
        }

        private async void ApproveButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var item = (LoginRequestItem)button.Tag;

            if (MessageBox.Show($"Approve login request from {item.UserName}?",
                "Confirm Approval", MessageBoxButton.YesNo,
         MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                await SendAcceptLoginAsync(item.LoginRequestId, 1); // 1 = Approved
            }
        }

        private async void RejectButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var item = (LoginRequestItem)button.Tag;

            if (MessageBox.Show($"Reject login request from {item.UserName}?",
"Confirm Rejection", MessageBoxButton.YesNo,
  MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                await SendAcceptLoginAsync(item.LoginRequestId, 2); // 2 = Rejected
            }
        }

        private async Task SendAcceptLoginAsync(Guid loginRequestId, int status)
        {
            try
            {
                var message = new
                {
                    Method = "AcceptLogin",
                    Data = new
                    {
                        LoginRequestId = loginRequestId,
                        Status = status
                    }
                };

                var json = JsonSerializer.Serialize(message);
                var bytes = Encoding.UTF8.GetBytes(json);
                await _stream!.WriteAsync(bytes, 0, bytes.Length);

                string action = status == 1 ? "approved" : "rejected";
                LogActivity($"📤 Login request {action}: {loginRequestId}");
            }
            catch (Exception ex)
            {
                LogActivity($"❌ Error sending response: {ex.Message}");
                _notifier?.ShowError($"❌ Failed to send response");
            }
        }

        private async void RefreshHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadLoginHistoryAsync();
        }

        private async void PrevPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                await LoadLoginHistoryAsync();
            }
        }

        private async void NextPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage < _totalPages)
            {
                _currentPage++;
                await LoadLoginHistoryAsync();
            }
        }

        #region User Management Methods

        private async Task LoadUsersAsync()
        {
            try
            {
                Console.WriteLine("📤 MainWindow: Loading users from database...");

                using (var scope = App.ServiceProvider.CreateScope())
                {
                    var userRepository = scope.ServiceProvider.GetRequiredService<IRepositoryAsync<Users>>();
                    var roleRepository = scope.ServiceProvider.GetRequiredService<IRepositoryAsync<Roles>>();

                    // Get "User" role
                    var userRole = await roleRepository.AsQueryable()
                        .Where(r => r.Name.ToLower() == "user")
                        .FirstOrDefaultAsync();

                    if (userRole == null)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            LogActivity("⚠️ User role not found in database");
                        });
                        return;
                    }

                    // Get all users with "User" role
                    var users = await userRepository.AsQueryable()
                        .Where(u => u.RoleId == userRole.Id)
                        .OrderByDescending(u => u.CreatedAt)
                        .ToListAsync();

                    Dispatcher.Invoke(() =>
                    {
                        _users.Clear();
                        _filteredUsers.Clear();

                        foreach (var user in users)
                        {
                            var item = new UserManagementItem
                            {
                                UserId = user.Id,
                                UserName = user.UserName,
                                FullName = user.FullName ?? "",
                                Email = user.Email,
                                PhoneNumber = user.PhoneNumber ?? "",
                                IsActive = user.IsActive,
                                CreatedAt = user.CreatedAt,
                                RoleId = user.RoleId
                            };
                            _users.Add(item);
                            _filteredUsers.Add(item);
                        }

                        // Update pagination
                        _usersTotalPages = (int)Math.Ceiling((double)_filteredUsers.Count / _usersRecordPerPage);
                        UpdateUsersPaginationUI();

                        TotalUsersTextRun.Text = _users.Count.ToString();

                        LogActivity($"✅ Loaded {_users.Count} users from database");
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ MainWindow: Failed to load users: {ex.Message}");
                Dispatcher.Invoke(() =>
                {
                    LogActivity($"❌ Error loading users: {ex.Message}");
                });
            }
        }

        private void UpdateUsersPaginationUI()
        {
            UsersCurrentPageTextRun.Text = _usersCurrentPage.ToString();
            UsersTotalPagesTextRun.Text = _usersTotalPages.ToString();
            UsersPrevPageButton.IsEnabled = _usersCurrentPage > 1;
            UsersNextPageButton.IsEnabled = _usersCurrentPage < _usersTotalPages;
        }

        private async void RefreshUsersButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadUsersAsync();
        }

        private async void UsersPrevPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (_usersCurrentPage > 1)
            {
                _usersCurrentPage--;
                UpdateUsersPaginationUI();
            }
        }

        private async void UsersNextPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (_usersCurrentPage < _usersTotalPages)
            {
                _usersCurrentPage++;
                UpdateUsersPaginationUI();
            }
        }

        private void UserSearchTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            var searchText = UserSearchTextBox.Text.ToLower();

            _filteredUsers.Clear();

            foreach (var user in _users)
            {
                if (string.IsNullOrWhiteSpace(searchText) ||
                    user.UserName.ToLower().Contains(searchText) ||
                    user.FullName.ToLower().Contains(searchText) ||
                    user.Email.ToLower().Contains(searchText))
                {
                    _filteredUsers.Add(user);
                }
            }

            // Update pagination after filter
            _usersTotalPages = (int)Math.Ceiling((double)_filteredUsers.Count / _usersRecordPerPage);
            _usersCurrentPage = 1;
            UpdateUsersPaginationUI();

            LogActivity($"🔍 Search: Found {_filteredUsers.Count} users matching '{searchText}'");
        }

        private void ViewUserButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var user = (UserManagementItem)button.Tag;

            var message = $"User Details:\n\n" +
                          $"Username: {user.UserName}\n" +
                          $"Full Name: {user.FullName}\n" +
                          $"Email: {user.Email}\n" +
                          $"Phone: {user.PhoneNumber}\n" +
                          $"Status: {(user.IsActive ? "Active" : "Locked")}\n" +
                          $"Created: {user.CreatedAt:yyyy-MM-dd HH:mm:ss}";

            MessageBox.Show(message, "User Details", MessageBoxButton.OK, MessageBoxImage.Information);

            LogActivity($"👁️ Viewed details for user: {user.UserName}");
        }

        private async void ToggleUserStatusButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var user = (UserManagementItem)button.Tag;

            var action = user.IsActive ? "lock" : "unlock";
            var result = MessageBox.Show(
                $"Are you sure you want to {action} user '{user.UserName}'?",
                $"Confirm {action.ToUpper()}",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var scope = App.ServiceProvider.CreateScope())
                    {
                        var userRepository = scope.ServiceProvider.GetRequiredService<IRepositoryAsync<Users>>();

                        var dbUser = await userRepository.AsQueryable()
                            .Where(u => u.Id == user.UserId)
                            .FirstOrDefaultAsync();

                        if (dbUser != null)
                        {
                            dbUser.IsActive = !dbUser.IsActive;
                            await userRepository.UpdateAsync(dbUser);

                            // Update UI
                            user.IsActive = dbUser.IsActive;
                            await LoadUsersAsync(); // Refresh list

                            LogActivity($"🔒 User {user.UserName} {(dbUser.IsActive ? "unlocked" : "locked")} successfully");
                            _notifier?.ShowSuccess($"User {user.UserName} {(dbUser.IsActive ? "unlocked" : "locked")} successfully");
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogActivity($"❌ Error toggling user status: {ex.Message}");
                    _notifier?.ShowError($"Failed to {action} user");
                }
            }
        }

        #endregion

        private void UpdatePaginationUI()
        {
            CurrentPageTextRun.Text = _currentPage.ToString();
            TotalPagesTextRun.Text = _totalPages.ToString();
            PrevPageButton.IsEnabled = _currentPage > 1;
            NextPageButton.IsEnabled = _currentPage < _totalPages;
        }

        private void UpdateConnectionStatus(bool isConnected)
        {
            Dispatcher.Invoke(() =>
            {
                if (isConnected)
                {
                    AdminStatusIndicator.Fill = new SolidColorBrush(Colors.LimeGreen);
                    AdminStatusText.Text = "Connected";
                }
                else
                {
                    AdminStatusIndicator.Fill = new SolidColorBrush(Colors.Red);
                    AdminStatusText.Text = "Disconnected";
                }
            });
        }

        private void UpdatePendingCount()
        {
            Dispatcher.Invoke(() =>
            {
                TotalPendingTextBlock.Text = _loginRequests.Count.ToString();
            });
        }

        private void LogActivity(string message)
        {
            Dispatcher.Invoke(() =>
            {
                var timestamp = DateTime.Now.ToString("HH:mm:ss");
                ActivityLogTextBlock.Text += $"[{timestamp}] {message}\n";
            });
        }

        protected override void OnClosed(EventArgs e)
        {
            _isRunning = false;
            _stream?.Close();
            _client?.Close();
            _notifier?.Dispose();
            base.OnClosed(e);
        }
    }

    // Model classes
    public class LoginRequestItem
    {
        public Guid LoginRequestId { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string? IpAddress { get; set; }
        public string? DeviceInfo { get; set; }
        public DateTime RequestedAt { get; set; }
        public int Status { get; set; }
    }

    public class LoginHistoryItem
    {
        public Guid LoginHistoryId { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } = "";
        public string FullName { get; set; } = "";
        public string IpAddress { get; set; } = "";
        public string DeviceInfo { get; set; } = "";
        public DateTime LoginTime { get; set; }
        public bool IsSuccessful { get; set; }
    }

    // Model class for User Management
    public class UserManagementItem
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string PhoneNumber { get; set; } = "";
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid RoleId { get; set; }
    }
}