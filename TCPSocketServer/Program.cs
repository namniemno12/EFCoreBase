using System;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json;

class TcpLoginServer
{
    static ConcurrentDictionary<string, string> users = new()
    {
        ["admin"] = HashPassword("123456"),
        ["son"] = HashPassword("abcdef"),
        ["test"] = HashPassword("111111")
    };

    static ClientWebSocket wsClient;

    static async Task Main()
    {
        // Kết nối tới WebSocket server
        wsClient = new ClientWebSocket();
        await wsClient.ConnectAsync(new Uri("wss://localhost:7015/ws/test"), CancellationToken.None);

        // Login as admin
        var adminLogin = new
        {
            i = 1,
            m = "admin_login",
            dt = new { username = "admin", password = "123456" }
        };
        await SendWebSocketMessage(JsonSerializer.Serialize(adminLogin));

        TcpListener server = new TcpListener(IPAddress.Any, 9000);
        server.Start();
        Console.WriteLine("✅ TCP Login Server đang chạy tại cổng 9000...\n");

        while (true)
        {
            var client = server.AcceptTcpClient();
            _ = HandleClient(client);
        }
    }

    static async Task HandleClient(TcpClient client)
    {
        var stream = client.GetStream();
        byte[] buffer = new byte[1024];
        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

        Console.WriteLine($"📩 Nhận từ client: {message}");

        string response;
        var parts = message.Split('|');

        if (parts.Length == 2)
        {
            string username = parts[0];
            string password = parts[1];

            if (users.TryGetValue(username, out string storedHash))
            {
                if (storedHash == HashPassword(password))
                {
                    response = $"success|Xin chào {username}!";
                    Console.WriteLine($"✅ {username} đăng nhập thành công!");

                    // Gửi thông báo qua WebSocket
                    var notification = new
                    {
                        i = 0,
                        m = "user_login_success",
                        dt = new { username = username }
                    };
                    await SendWebSocketMessage(JsonSerializer.Serialize(notification));
                }
                else
                {
                    response = "failed|Sai mật khẩu!";
                    Console.WriteLine($"❌ {username} nhập sai mật khẩu!");
                }
            }
            else
            {
                response = "failed|Không tồn tại người dùng này!";
                Console.WriteLine($"⚠️ Không tìm thấy {username} trong hệ thống!");
            }
        }
        else
        {
            response = "failed|Sai định dạng. Gửi dạng: username|password";
        }

        byte[] data = Encoding.UTF8.GetBytes(response);
        await stream.WriteAsync(data, 0, data.Length);

        client.Close();
    }

    static async Task SendWebSocketMessage(string message)
    {
        try
        {
            if (wsClient.State == WebSocketState.Open)
            {
                var bytes = Encoding.UTF8.GetBytes(message);
                await wsClient.SendAsync(
                    new ArraySegment<byte>(bytes),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Lỗi gửi WebSocket: {ex.Message}");
        }
    }

    static string HashPassword(string password)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
        return BitConverter.ToString(bytes).Replace("-", "").ToLower();
    }
}