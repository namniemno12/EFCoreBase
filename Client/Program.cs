using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace ClientTestConsole
{
    class Program
    {
        private static TcpClient? _client;
        private static NetworkStream? _stream;
        private static bool _isRunning = true;
        private static string _userName = "TestUser";
        private static Guid _userId = Guid.NewGuid();

        static async Task Main(string[] args)
        {
            Console.WriteLine("👤 Client Test Console");
            Console.WriteLine("======================\n");

            Console.Write("Enter your username (default: TestUser): ");
            var input = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(input))
                _userName = input;

            Console.WriteLine($"\n👤 Username: {_userName}");
            Console.WriteLine($"🆔 UserId: {_userId}\n");

            try
            {
                // Connect to server
                _client = new TcpClient();
                await _client.ConnectAsync("localhost", 9000);
                _stream = _client.GetStream();
                Console.WriteLine("✅ Connected to TCP Server\n");

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
                        case "LoginRequestAck":
                            var ackData = doc.RootElement.GetProperty("Data");
                            var ackMsg = ackData.GetProperty("Message").GetString();
                            var reqId = ackData.GetProperty("LoginRequestId").GetString();
                            Console.WriteLine($"\n✅ {ackMsg}");
                            Console.WriteLine($"📝 Login Request ID: {reqId}");
                            Console.WriteLine("⏳ Waiting for admin approval...");
                            break;

                        case "LoginResult":
                            var resultData = doc.RootElement.GetProperty("Data");
                            var isSuccess = resultData.GetProperty("IsSuccess").GetBoolean();
                            var message = resultData.GetProperty("Message").GetString();

                            if (isSuccess)
                            {
                                Console.WriteLine($"\n🎉 LOGIN APPROVED! 🎉");
                                Console.WriteLine($"✅ {message}");
                                if (resultData.TryGetProperty("ApprovedBy", out var approvedBy))
                                {
                                    Console.WriteLine($"👔 Approved by: {approvedBy.GetString()}");
                                }
                            }
                            else
                            {
                                Console.WriteLine($"\n❌ LOGIN REJECTED");
                                Console.WriteLine($"⚠️ {message}");
                                if (resultData.TryGetProperty("RejectedBy", out var rejectedBy))
                                {
                                    Console.WriteLine($"👔 Rejected by: {rejectedBy.GetString()}");
                                }
                            }
                            break;

                        case "Error":
                            var errorMsg = doc.RootElement.GetProperty("Data").GetProperty("Message").GetString();
                            Console.WriteLine($"⚠️ Error from server: {errorMsg}");
                            break;
                    }

                    Console.Write("\nClient> ");
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
            Console.WriteLine("📝 Commands:");
            Console.WriteLine("   login  - Send login request");
            Console.WriteLine("   exit   - Exit\n");

            while (_isRunning)
            {
                Console.Write("Client> ");
                var input = Console.ReadLine()?.Trim().ToLower();

                if (string.IsNullOrEmpty(input)) continue;

                try
                {
                    switch (input)
                    {
                        case "login":
                            await SendLoginRequestAsync();
                            break;

                        case "exit":
                            _isRunning = false;
                            Console.WriteLine("👋 Exiting...");
                            return;

                        default:
                            Console.WriteLine("⚠️ Unknown command. Type 'login' or 'exit'");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error: {ex.Message}");
                }
            }
        }

        static async Task SendLoginRequestAsync()
        {
            var message = new
            {
                Method = "LoginRequest",
                Data = new
                {
                    UserId = _userId,
                    UserName = _userName,
                    IpAddress = "127.0.0.1",
                    DeviceInfo = "Windows 10 - Test Console"
                }
            };

            await SendMessageAsync(message);
            Console.WriteLine($"📤 Login request sent for user: {_userName}");
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
