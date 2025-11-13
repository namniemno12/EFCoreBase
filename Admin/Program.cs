using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace AdminTestConsole
{
    class Program
    {
        private static TcpClient? _client;
        private static NetworkStream? _stream;
        private static bool _isRunning = true;
        private static string _adminName = "Admin1";

        static async Task Main(string[] args)
        {
            Console.WriteLine("👔 Admin Test Console");
            Console.WriteLine("=====================\n");

            try
            {
                // Connect to server
                _client = new TcpClient();
                await _client.ConnectAsync("localhost", 9000);
                _stream = _client.GetStream();
                Console.WriteLine("✅ Connected to TCP Server\n");

                // Send AdminConnect
                await SendAdminConnectAsync();

                // Start listening for messages
                var listenTask = Task.Run(ListenForMessagesAsync);

                // Handle user commands
                await HandleUserInputAsync();

                _isRunning = false;
                await listenTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
            }
            finally
            {
                _stream?.Close();
                _client?.Close();
            }
        }

        static async Task SendAdminConnectAsync()
        {
            var message = new
            {
                Method = "AdminConnect",
                Data = new { AdminName = _adminName }
            };

            await SendMessageAsync(message);
            Console.WriteLine($"📤 Sent AdminConnect for {_adminName}\n");
        }

        static async Task ListenForMessagesAsync()
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
                    Console.WriteLine($"\n📩 Received: {json}");

                    var doc = JsonDocument.Parse(json);
                    var method = doc.RootElement.GetProperty("Method").GetString();

                    switch (method)
                    {
                        case "AdminConnectAck":
                            Console.WriteLine("✅ Admin connected successfully!");
                            break;

                        case "NewLoginRequest":
                            var data = doc.RootElement.GetProperty("Data");
                            var userName = data.GetProperty("UserName").GetString();
                            var loginRequestId = data.GetProperty("LoginRequestId").GetString();
                            Console.WriteLine($"\n🔔 NEW LOGIN REQUEST:");
                            Console.WriteLine($"   User: {userName}");
                            Console.WriteLine($"   RequestId: {loginRequestId}");
                            Console.WriteLine($"   Type 'accept {loginRequestId}' or 'reject {loginRequestId} <reason>' to respond");
                            break;

                        case "PendingLoginRequests":
                            var requestsData = doc.RootElement.GetProperty("Data");
                            var count = requestsData.GetProperty("Count").GetInt32();
                            Console.WriteLine($"\n📋 Pending Login Requests: {count}");
                            if (count > 0)
                            {
                                var requests = requestsData.GetProperty("Requests");
                                foreach (var req in requests.EnumerateArray())
                                {
                                    Console.WriteLine($"   - User: {req.GetProperty("UserName").GetString()}");
                                    Console.WriteLine($"     ID: {req.GetProperty("LoginRequestId").GetString()}");
                                }
                            }
                            break;

                        case "AcceptLoginAck":
                            Console.WriteLine("✅ Login request accepted successfully!");
                            break;

                        case "RejectLoginAck":
                            Console.WriteLine("❌ Login request rejected successfully!");
                            break;

                        case "Error":
                            var errorMsg = doc.RootElement.GetProperty("Data").GetProperty("Message").GetString();
                            Console.WriteLine($"⚠️ Error from server: {errorMsg}");
                            break;
                    }

                    Console.Write("\nAdmin> ");
                }
                catch (Exception ex)
                {
                    if (_isRunning)
                        Console.WriteLine($"⚠️ Listen error: {ex.Message}");
                    break;
                }
            }
        }

        static async Task HandleUserInputAsync()
        {
            Console.WriteLine("\n📝 Commands:");
            Console.WriteLine("   list     - Get pending requests");
            Console.WriteLine("   accept <requestId>          - Accept login request");
            Console.WriteLine("   reject <requestId> <reason>   - Reject login request");
            Console.WriteLine("   exit      - Exit\n");

            while (_isRunning)
            {
                Console.Write("Admin> ");
                var input = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(input)) continue;

                var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var command = parts[0].ToLower();

                try
                {
                    switch (command)
                    {
                        case "list":
                            await SendGetPendingRequestsAsync();
                            break;

                        case "accept":
                            if (parts.Length < 2)
                            {
                                Console.WriteLine("⚠️ Usage: accept <requestId>");
                                break;
                            }
                            await SendAcceptLoginAsync(parts[1]);
                            break;

                        case "reject":
                            if (parts.Length < 2)
                            {
                                Console.WriteLine("⚠️ Usage: reject <requestId> <reason>");
                                break;
                            }
                            var reason = parts.Length > 2 ? string.Join(" ", parts.Skip(2)) : "Rejected by admin";
                            await SendRejectLoginAsync(parts[1], reason);
                            break;

                        case "exit":
                            _isRunning = false;
                            Console.WriteLine("👋 Exiting...");
                            return;

                        default:
                            Console.WriteLine("⚠️ Unknown command. Type 'list', 'accept', 'reject', or 'exit'");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error: {ex.Message}");
                }
            }
        }

        static async Task SendGetPendingRequestsAsync()
        {
            var message = new
            {
                Method = "GetPendingRequests",
                Data = new { }
            };
            await SendMessageAsync(message);
        }

        static async Task SendAcceptLoginAsync(string loginRequestId)
        {
            var message = new
            {
                Method = "AcceptLogin",
                Data = new { LoginRequestId = loginRequestId }
            };
            await SendMessageAsync(message);
            Console.WriteLine($"📤 Sent AcceptLogin for {loginRequestId}");
        }

        static async Task SendRejectLoginAsync(string loginRequestId, string reason)
        {
            var message = new
            {
                Method = "RejectLogin",
                Data = new { LoginRequestId = loginRequestId, Reason = reason }
            };
            await SendMessageAsync(message);
            Console.WriteLine($"📤 Sent RejectLogin for {loginRequestId}");
        }

        static async Task SendMessageAsync(object message)
        {
            if (_stream == null) return;

            var json = JsonSerializer.Serialize(message);
            var bytes = Encoding.UTF8.GetBytes(json);
            await _stream.WriteAsync(bytes, 0, bytes.Length);
        }
    }
}
