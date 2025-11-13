using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyProject.Application.Services;
using MyProject.Application.Services.Interfaces;
using MyProject.Domain.DTOs.Auth.Req;
using MyProject.Domain.DTOs.Auth.Res;
using MyProject.Domain.Entities;
using MyProject.Helper.Utils;
using MyProject.Helper.Utils.Interfaces;
using MyProject.Infrastructure;
using MyProject.Infrastructure.Persistence.HandleContext;

namespace TcpSocketServer
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("🚀 Starting TCP Socket Server with Database Integration...");

            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    var configuration = context.Configuration;

                    // Add DbContext with SQL Server
                    services.AddDbContext<ApplicationDbContext>(options =>
options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

                    // Register DbContext for RepositoryAsync
                    services.AddScoped<DbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

                    // Add Repositories
                    services.AddScoped<IRepositoryAsync<Users>, RepositoryAsync<Users>>();
                    services.AddScoped<IRepositoryAsync<Roles>, RepositoryAsync<Roles>>();
                    services.AddScoped<IRepositoryAsync<LoginHistory>, RepositoryAsync<LoginHistory>>();
                    services.AddScoped<IRepositoryAsync<LoginRequest>, RepositoryAsync<LoginRequest>>();

                    // ✅ Add JWT Settings Configuration
                    services.Configure<MyProject.Helper.ModelHelps.JwtSettings>(
 configuration.GetSection("Jwt"));

                    // Add Utilities
                    services.AddSingleton<CryptoHelperUtil>();
                    services.AddSingleton<ITokenUtils, TokenUtils>();

                    // Add Services
                    // NOTE: TCP Server không cần ITcpSocketService và IHttpContextAccessor
                    // Chúng sẽ được resolve = null trong constructor
                    services.AddScoped<IAuthServices, AuthServices>();

                    // Add TCP Server as singleton
                    services.AddSingleton<TcpSocketServerService>();
                })
                .Build();

            // Get the TCP server service
            var server = host.Services.GetRequiredService<TcpSocketServerService>();
            await server.StartAsync(CancellationToken.None);

            await host.RunAsync();
        }
    }

    public class TcpSocketServerService
    {
        private readonly int _port;
        private TcpListener? _listener;
        private readonly IServiceProvider _serviceProvider;

        // Lưu kết nối của user: key = UserName
        private readonly ConcurrentDictionary<string, ClientConnection> _userConnections = new();

        // Lưu kết nối của admin
        private readonly ConcurrentDictionary<string, ClientConnection> _adminConnections = new();

        // Lưu login requests đang pending: key = LoginRequestId
        private readonly ConcurrentDictionary<Guid, LoginRequestData> _pendingLoginRequests = new();

        public TcpSocketServerService(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _port = configuration.GetValue<int>("TcpServer:Port", 9000);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start();
            Console.WriteLine($"✅ TCP Socket Server started on port {_port}");
            Console.WriteLine($"📡 Listening for connections...\n");

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var client = await _listener.AcceptTcpClientAsync(cancellationToken);
                        var endpoint = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
                        Console.WriteLine($"🔌 New client connected from {endpoint}");

                        var connection = new ClientConnection(client);
                        _ = Task.Run(() => HandleClientAsync(connection, cancellationToken), cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ TCP Accept error: {ex.Message}");
                    }
                }
            }
            finally
            {
                _listener.Stop();
                Console.WriteLine("⛔ TCP Socket Server stopped");
            }
        }

        private async Task HandleClientAsync(ClientConnection connection, CancellationToken cancellationToken)
        {
            using (connection.Client)
            using (connection.Stream)
            {
                var buffer = new byte[8192];

                try
                {
                    while (!cancellationToken.IsCancellationRequested && connection.Client.Connected)
                    {
                        int bytesRead;
                        try
                        {
                            bytesRead = await connection.Stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"⚠️ TCP read error: {ex.Message}");
                            break;
                        }

                        if (bytesRead <= 0)
                        {
                            Console.WriteLine($"🔌 Client {connection.ConnectionId} disconnected");
                            break;
                        }

                        var message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        Console.WriteLine($"📩 Received from {connection.ConnectionId}: {message}");

                        await ProcessMessageAsync(message, connection);
                    }
                }
                finally
                {
                    // Cleanup khi disconnect
                    if (!string.IsNullOrEmpty(connection.UserName))
                    {
                        if (connection.IsAdmin)
                        {
                            _adminConnections.TryRemove(connection.UserName, out _);
                            Console.WriteLine($"👔 Admin {connection.UserName} disconnected");
                        }
                        else
                        {
                            _userConnections.TryRemove(connection.UserName, out _);
                            Console.WriteLine($"👤 User {connection.UserName} disconnected");
                        }
                    }
                }
            }
        }

        private async Task ProcessMessageAsync(string message, ClientConnection connection)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                // Parse thành Dictionary để lấy Method và Data riêng biệt
                using var doc = JsonDocument.Parse(message);
                var root = doc.RootElement;

                if (!root.TryGetProperty("Method", out var methodElement))
                {
                    Console.WriteLine("⚠️ Cannot find Method in message");
                    await SendErrorAsync(connection.Stream, "Invalid message format");
                    return;
                }

                var method = methodElement.GetString() ?? string.Empty;
                Console.WriteLine($"📝 Processing method: {method}");

                if (!root.TryGetProperty("Data", out var dataElement))
                {
                    Console.WriteLine("⚠️ Cannot find Data in message");
                    await SendErrorAsync(connection.Stream, "Invalid message format");
                    return;
                }

                switch (method)
                {
                    case "LoginRequest":
                        await HandleLoginRequestAsync(dataElement, connection, options);
                        break;

                    case "AdminLogin": // ✅ Admin login qua TCP (nếu cần)
                        await HandleAdminLoginAsync(dataElement, connection, options);
                        break;

                    case "AdminConnect": // ✅ Admin đã login qua UI, giờ kết nối TCP
                    await HandleAdminConnectAsync(dataElement, connection, options);
                    break;

                    case "AcceptLogin":
                        await HandleAcceptLoginAsync(dataElement, connection, options);
                        break;

                    case "RejectLogin":
                        await HandleRejectLoginAsync(dataElement, connection, options);
                        break;

                    case "GetPendingRequests":
                        await HandleGetPendingRequestsAsync(connection);
                        break;

                    default:
                        Console.WriteLine($"ℹ️ Unknown method: {method}");
                        await SendErrorAsync(connection.Stream, $"Unknown method: {method}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error processing message: {ex.Message}");
                await SendErrorAsync(connection.Stream, "Error processing message");
            }
        }

        #region Handlers

        /// <summary>
        /// Xử lý LoginRequest từ Client - Validate account trước khi lưu vào database
        /// </summary>
        private async Task HandleLoginRequestAsync(JsonElement dataElement, ClientConnection connection, JsonSerializerOptions options)
        {
            try
            {
                var loginData = dataElement.Deserialize<LoginDataReq>(options);
                if (loginData == null || string.IsNullOrEmpty(loginData.UserName) || string.IsNullOrEmpty(loginData.Password))
                {
                    await SendErrorAsync(connection.Stream, "Invalid login data - Username and Password required");
                    return;
                }

                Console.WriteLine($"🔐 Validating account: {loginData.UserName}");

                // Validate account credentials thông qua AuthServices
                using (var scope = _serviceProvider.CreateScope())
                {
                    var authServices = scope.ServiceProvider.GetRequiredService<IAuthServices>();
                    var userRepository = scope.ServiceProvider.GetRequiredService<IRepositoryAsync<Users>>();
                    var cryptoHelper = scope.ServiceProvider.GetRequiredService<CryptoHelperUtil>();

                    // 1. Kiểm tra user tồn tại
                    var user = await userRepository.AsQueryable()
                     .Where(x => x.UserName == loginData.UserName)
           .FirstOrDefaultAsync();

                    if (user == null)
                    {
                        Console.WriteLine($"❌ User not found: {loginData.UserName}");

                        var errorResponse = new MessageEnvelope
                        {
                            Method = "LoginRequestAck",
                            Data = new
                            {
                                IsSuccess = false,
                                Message = "❌ Invalid username or password!",
                                LoginRequestId = (Guid?)null
                            }
                        };

                        await SendJsonAsync(connection.Stream, errorResponse);
                        return;
                    }

                    // 2. Decrypt password từ DB và so sánh
                    string decryptedPassword;
                    try
                    {
                        decryptedPassword = cryptoHelper.Decrypt(user.PasswordHash);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Failed to decrypt password: {ex.Message}");

                        var errorResponse = new MessageEnvelope
                        {
                            Method = "LoginRequestAck",
                            Data = new
                            {
                                IsSuccess = false,
                                Message = "❌ Invalid username or password!",
                                LoginRequestId = (Guid?)null
                            }
                        };

                        await SendJsonAsync(connection.Stream, errorResponse);
                        return;
                    }

                    if (decryptedPassword != loginData.Password)
                    {
                        Console.WriteLine($"❌ Invalid password for user: {loginData.UserName}");

                        var errorResponse = new MessageEnvelope
                        {
                            Method = "LoginRequestAck",
                            Data = new
                            {
                                IsSuccess = false,
                                Message = "❌ Invalid username or password!",
                                LoginRequestId = (Guid?)null
                            }
                        };

                        await SendJsonAsync(connection.Stream, errorResponse);
                        return;
                    }

                    // 3. Kiểm tra account bị khóa
                    if (!user.IsActive)
                    {
                        Console.WriteLine($"⚠️ Account is locked: {loginData.UserName}");

                        var errorResponse = new MessageEnvelope
                        {
                            Method = "LoginRequestAck",
                            Data = new
                            {
                                IsSuccess = false,
                                Message = "⚠️ Your account has been locked. Please contact administrator.",
                                LoginRequestId = (Guid?)null
                            }
                        };

                        await SendJsonAsync(connection.Stream, errorResponse);
                        return;
                    }

                    // 4. Credentials valid → Lưu thông tin connection
                    connection.UserId = user.Id;
                    connection.UserName = loginData.UserName;
                    connection.IsAdmin = false;

                    // Thêm vào danh sách user connections
                    _userConnections[loginData.UserName] = connection;

                    Console.WriteLine($"✅ Account validated successfully: {loginData.UserName} (ID: {user.Id})");

                    // 5. Tạo LoginRequest trong DB
                    var addLoginRequestReq = new AddLoginRequestReq
                    {
                        UserId = user.Id,
                        RequestedAt = DateTime.UtcNow,
                        Status = 0, // Pending
                        IpAddress = loginData.IpAddress,
                        DeviceInfo = loginData.DeviceInfo
                    };

                    var dbResult = await authServices.AddLoginRequest(addLoginRequestReq);

                    if (dbResult.ResponseCode != (int)MyProject.Helper.Constants.Globals.ResponseCodeEnum.SUCCESS)
                    {
                        Console.WriteLine($"❌ Failed to save login request to DB: {dbResult.Message}");
                        await SendErrorAsync(connection.Stream, dbResult.Message);
                        return;
                    }

                    var loginRequestId = dbResult.Data.LoginRequestId;
                    Console.WriteLine($"💾 Login request saved to database with ID: {loginRequestId}");

                    // 6. Lưu vào memory để tracking
                    var loginRequest = new LoginRequestData
                    {
                        LoginRequestId = loginRequestId,
                        UserId = user.Id,
                        UserName = loginData.UserName,
                        IpAddress = loginData.IpAddress,
                        DeviceInfo = loginData.DeviceInfo,
                        RequestedAt = DateTime.UtcNow,
                        Status = 0 // Pending
                    };

                    _pendingLoginRequests[loginRequestId] = loginRequest;

                    Console.WriteLine($"👤 Login request from user: {loginData.UserName} (ID: {loginRequestId})");

                    // 7. Gửi ACK cho user
                    var userResponse = new MessageEnvelope
                    {
                        Method = "LoginRequestAck",
                        Data = new
                        {
                            IsSuccess = true,
                            Message = "✅ Account verified! Waiting for admin approval...",
                            LoginRequestId = loginRequestId
                        }
                    };

                    await SendJsonAsync(connection.Stream, userResponse);

                    // 8. Broadcast đến tất cả admin
                    await BroadcastToAdminsAsync(new MessageEnvelope
                    {
                        Method = "NewLoginRequest",
                        Data = loginRequest
                    });

                    Console.WriteLine($"✉️ Login request sent to {_adminConnections.Count} admin(s)");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ HandleLoginRequest error: {ex.Message}");
                Console.WriteLine($"⚠️ Stack trace: {ex.StackTrace}");
                await SendErrorAsync(connection.Stream, "Error processing login request");
            }
        }

        /// <summary>
        /// Xử lý đăng nhập từ Admin - Gọi AuthServices.LoginByAdmin
        /// </summary>
        private async Task HandleAdminLoginAsync(JsonElement dataElement, ClientConnection connection, JsonSerializerOptions options)
        {
            try
            {
                var loginData = dataElement.Deserialize<LoginCmsRequest>(options);
                if (loginData == null || string.IsNullOrEmpty(loginData.UserName) || string.IsNullOrEmpty(loginData.Password))
                {
                    await SendErrorAsync(connection.Stream, "Invalid login data - Username and Password required");
                    return;
                }

                Console.WriteLine($"🔐 Admin login attempt: {loginData.UserName}");

                using (var scope = _serviceProvider.CreateScope())
                {
                    var authServices = scope.ServiceProvider.GetRequiredService<IAuthServices>();

                    // Gọi LoginByAdmin từ AuthServices
                    var loginResult = await authServices.LoginByAdmin(loginData);

                    if (loginResult.ResponseCode != (int)MyProject.Helper.Constants.Globals.ResponseCodeEnum.SUCCESS)
                    {
                        Console.WriteLine($"❌ Admin login failed: {loginResult.Message}");

                        var errorResponse = new MessageEnvelope
                        {
                            Method = "AdminLoginAck",
                            Data = new
                            {
                                IsSuccess = false,
                                Message = loginResult.Message
                            }
                        };

                        await SendJsonAsync(connection.Stream, errorResponse);
                        return;
                    }

                    // ✅ Login thành công
                    var userRepository = scope.ServiceProvider.GetRequiredService<IRepositoryAsync<Users>>();
                    var admin = await userRepository.AsQueryable()
                 .Where(x => x.UserName == loginData.UserName)
                   .FirstOrDefaultAsync();

                    if (admin == null)
                    {
                        await SendErrorAsync(connection.Stream, "Admin not found after successful login");
                        return;
                    }

                    // Lưu thông tin admin connection
                    connection.UserId = admin.Id;
                    connection.AdminId = admin.Id;
                    connection.UserName = loginData.UserName;
                    connection.IsAdmin = true;

                    _adminConnections[loginData.UserName] = connection;

                    Console.WriteLine($"✅ Admin logged in successfully: {loginData.UserName} (ID: {admin.Id})");

                    // Gửi response với tokens
                    var response = new MessageEnvelope
                    {
                        Method = "AdminLoginAck",
                        Data = new
                        {
                            IsSuccess = true,
                            Message = "✅ Đăng nhập thành công!",
                            AdminId = admin.Id,
                            AdminName = admin.UserName,
                            FullName = admin.FullName,
                            AccessToken = loginResult.Data.AccessToken,
                            RefreshToken = loginResult.Data.RefreshToken,
                            PendingRequestsCount = _pendingLoginRequests.Count
                        }
                    };

                    await SendJsonAsync(connection.Stream, response);

                    // Tự động gửi danh sách pending requests
                    await HandleGetPendingRequestsAsync(connection);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ HandleAdminLogin error: {ex.Message}");
                Console.WriteLine($"⚠️ Stack trace: {ex.StackTrace}");
                await SendErrorAsync(connection.Stream, "Error processing admin login");
            }
        }
        /// <summary>
        /// Xử lý AdminConnect - Admin đã login qua UI, giờ register TCP connection
        /// </summary>
        private async Task HandleAdminConnectAsync(JsonElement dataElement, ClientConnection connection, JsonSerializerOptions options)
        {
            try
            {
                var adminData = dataElement.Deserialize<AdminConnectData>(options);
                if (adminData == null || string.IsNullOrEmpty(adminData.AdminName) || adminData.AdminId == Guid.Empty)
                {
                    await SendErrorAsync(connection.Stream, "Invalid admin data - AdminName and AdminId required");
                    return;
                }

                Console.WriteLine($"👔 Admin connecting via TCP: {adminData.AdminName} (ID: {adminData.AdminId})");

                // ✅ Validate AccessToken (optional - bạn có thể validate token ở đây)
                if (string.IsNullOrEmpty(adminData.AccessToken))
                {
                    await SendErrorAsync(connection.Stream, "AccessToken is required");
                    return;
                }

                // Lưu thông tin admin connection
                connection.UserId = adminData.AdminId;
                connection.AdminId = adminData.AdminId;
                connection.UserName = adminData.AdminName;
                connection.IsAdmin = true;

                _adminConnections[adminData.AdminName] = connection;

                Console.WriteLine($"✅ Admin connected successfully: {adminData.AdminName} (ID: {adminData.AdminId})");
                Console.WriteLine($"📊 Total admins connected: {_adminConnections.Count}");

                // Gửi response
                var response = new MessageEnvelope
                {
                    Method = "AdminConnectAck",
                    Data = new
                    {
                        IsSuccess = true,
                        Message = $"✅ Admin {adminData.AdminName} connected successfully!",
                        AdminId = adminData.AdminId,
                        PendingRequestsCount = _pendingLoginRequests.Count
                    }
                };

                await SendJsonAsync(connection.Stream, response);

                // Tự động gửi danh sách pending requests
                await HandleGetPendingRequestsAsync(connection);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ HandleAdminConnect error: {ex.Message}");
                Console.WriteLine($"⚠️ Stack trace: {ex.StackTrace}");
                await SendErrorAsync(connection.Stream, "Error processing admin connection");
            }
        }

        /// <summary>
        /// Xử lý Accept/Reject Login từ Admin - Lưu vào database
        /// AuthServices chỉ update database, TCP Server tự handle việc broadcast
        /// </summary>
        private async Task HandleAcceptLoginAsync(JsonElement dataElement, ClientConnection connection, JsonSerializerOptions options)
        {
            try
            {
                var acceptData = dataElement.Deserialize<AcceptLoginData>(options);
                if (acceptData == null)
                {
                    await SendErrorAsync(connection.Stream, "Invalid accept data");
                    return;
                }

                if (!_pendingLoginRequests.TryGetValue(acceptData.LoginRequestId, out var loginRequest))
                {
                    await SendErrorAsync(connection.Stream, "Login request not found");
                    return;
                }

                Console.WriteLine($"✅ Admin {connection.UserName} is processing login for user: {loginRequest.UserName}");

                // Cập nhật trạng thái trong database qua AuthServices
                // AuthServices CHỈ update database, KHÔNG gửi TCP message
                using (var scope = _serviceProvider.CreateScope())
                {
                    var authServices = scope.ServiceProvider.GetRequiredService<IAuthServices>();

                    var acceptLoginReq = new AcceptLoginRequestReq
                    {
                        LoginRequestId = acceptData.LoginRequestId,
                        Status = acceptData.Status // 1 = approved, 2 = rejected
                    };

                    var dbResult = await authServices.AcceptLoginRequest(connection.AdminId, acceptLoginReq);

                    if (dbResult.ResponseCode != (int)MyProject.Helper.Constants.Globals.ResponseCodeEnum.SUCCESS)
                    {
                        Console.WriteLine($"❌ Failed to update login request in DB: {dbResult.Message}");
                        await SendErrorAsync(connection.Stream, dbResult.Message);
                        return;
                    }

                    Console.WriteLine($"💾 Login request status updated in database");

                    // Nếu approved, lưu LoginHistory
                    if (acceptData.Status == 1)
                    {
                        var addHistoryReq = new AddLoginHistoryReq
                        {
                            UserId = loginRequest.UserId,
                            LoginTime = DateTime.UtcNow,
                            IpAddress = loginRequest.IpAddress,
                            DeviceInfo = loginRequest.DeviceInfo,
                            IsSuccessful = true
                        };

                        var historyResult = await authServices.AddLoginHistory(addHistoryReq);
                        if (historyResult.ResponseCode == (int)MyProject.Helper.Constants.Globals.ResponseCodeEnum.SUCCESS)
                        {
                            Console.WriteLine($"💾 Login history saved with ID: {historyResult.Data}");
                        }
                    }
                }

                // Remove từ pending
                _pendingLoginRequests.TryRemove(acceptData.LoginRequestId, out _);

                // TCP Server tự handle việc gửi kết quả cho user (không qua AuthServices)
                if (_userConnections.TryGetValue(loginRequest.UserName, out var userConn))
                {
                    var userResponse = new MessageEnvelope
                    {
                        Method = "LoginResult",
                        Data = new
                        {
                            IsSuccess = acceptData.Status == 1,
                            Message = acceptData.Status == 1 ? "Login approved by admin" : "Login rejected by admin",
                            LoginRequestId = acceptData.LoginRequestId,
                            ApprovedBy = connection.UserName,
                            ApprovedAt = DateTime.UtcNow,
                            Status = acceptData.Status
                        }
                    };

                    await SendJsonAsync(userConn.Stream, userResponse);
                    Console.WriteLine($"✉️ Result sent to user: {loginRequest.UserName}");
                }
                else
                {
                    Console.WriteLine($"⚠️ User {loginRequest.UserName} is offline");
                }

                // Gửi ACK cho admin
                var adminResponse = new MessageEnvelope
                {
                    Method = "AcceptLoginAck",
                    Data = new
                    {
                        IsSuccess = true,
                        Message = acceptData.Status == 1 ? "Login request accepted" : "Login request rejected",
                        LoginRequestId = acceptData.LoginRequestId
                    }
                };

                await SendJsonAsync(connection.Stream, adminResponse);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ HandleAcceptLogin error: {ex.Message}");
                Console.WriteLine($"⚠️ Stack trace: {ex.StackTrace}");
                await SendErrorAsync(connection.Stream, "Error accepting login");
            }
        }

        /// <summary>
        /// Xử lý Reject Login từ Admin
        /// </summary>
        private async Task HandleRejectLoginAsync(JsonElement dataElement, ClientConnection connection, JsonSerializerOptions options)
        {
            try
            {
                var rejectData = dataElement.Deserialize<RejectLoginData>(options);
                if (rejectData == null)
                {
                    await SendErrorAsync(connection.Stream, "Invalid reject data");
                    return;
                }

                // Convert to AcceptLoginData with Status = 2 (rejected)
                var acceptData = new AcceptLoginData
                {
                    LoginRequestId = rejectData.LoginRequestId,
                    Status = 2 // Rejected
                };

                // Reuse HandleAcceptLoginAsync logic
                var jsonElement = JsonSerializer.SerializeToElement(acceptData);
                await HandleAcceptLoginAsync(jsonElement, connection, options);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ HandleRejectLogin error: {ex.Message}");
                await SendErrorAsync(connection.Stream, "Error rejecting login");
            }
        }

        /// <summary>
        /// Gửi danh sách pending requests cho Admin
        /// </summary>
        private async Task HandleGetPendingRequestsAsync(ClientConnection connection)
        {
            try
            {
                var pendingRequests = _pendingLoginRequests.Values.ToList();

                var response = new MessageEnvelope
                {
                    Method = "PendingLoginRequests",
                    Data = new
                    {
                        Count = pendingRequests.Count,
                        Requests = pendingRequests
                    }
                };

                await SendJsonAsync(connection.Stream, response);
                Console.WriteLine($"📋 Sent {pendingRequests.Count} pending request(s) to admin");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ HandleGetPendingRequests error: {ex.Message}");
                await SendErrorAsync(connection.Stream, "Error getting pending requests");
            }
        }

        #endregion

        #region Helper Methods

        private async Task SendJsonAsync(NetworkStream stream, object obj)
        {
            try
            {
                var json = JsonSerializer.Serialize(obj);
                var bytes = Encoding.UTF8.GetBytes(json);
                await stream.WriteAsync(bytes, 0, bytes.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error sending JSON: {ex.Message}");
            }
        }

        private Task SendErrorAsync(NetworkStream stream, string message)
        {
            var error = new MessageEnvelope
            {
                Method = "Error",
                Data = new { Message = message }
            };

            return SendJsonAsync(stream, error);
        }

        private async Task BroadcastToAdminsAsync(MessageEnvelope message)
        {
            var tasks = _adminConnections.Values
                .Where(c => c.Client.Connected)
                .Select(c => SendJsonAsync(c.Stream, message));

            await Task.WhenAll(tasks);
        }

        #endregion

        #region Inner Classes

        private class ClientConnection
        {
            public string ConnectionId { get; }
            public TcpClient Client { get; }
            public NetworkStream Stream { get; }
            public Guid UserId { get; set; }
            public Guid AdminId { get; set; }
            public string? UserName { get; set; }
            public bool IsAdmin { get; set; }

            public ClientConnection(TcpClient client)
            {
                ConnectionId = Guid.NewGuid().ToString("N")[..8];
                Client = client;
                Stream = client.GetStream();
            }
        }

        private class MessageEnvelope
        {
            public string Method { get; set; } = string.Empty;
            public object Data { get; set; } = new();
        }

        private class LoginRequestData
        {
            public Guid LoginRequestId { get; set; }
            public Guid UserId { get; set; }
            public string UserName { get; set; } = string.Empty;
            public string? IpAddress { get; set; }
            public string? DeviceInfo { get; set; }
            public DateTime RequestedAt { get; set; }
            public int Status { get; set; }
        }

        private class AdminConnectData
        {
            public string AdminName { get; set; } = string.Empty;
            public Guid AdminId { get; set; }
            public string? AccessToken { get; set; }
        }

        private class AcceptLoginData
        {
            public Guid LoginRequestId { get; set; }
            public int Status { get; set; } // 1 = approved, 2 = rejected
        }

        private class RejectLoginData
        {
            public Guid LoginRequestId { get; set; }
            public string? Reason { get; set; }
        }

        #endregion
    }
}