using System;
using System.Net.Sockets;
using System.Text;

class TcpLoginClient
{
    static void Main()
    {
        Console.WriteLine("🌐 Kết nối tới TCP Login Server...");
        Console.Write("Nhập username: ");
        string username = Console.ReadLine();
        Console.Write("Nhập password: ");
        string password = Console.ReadLine();

        try
        {
            using var client = new TcpClient("127.0.0.1", 9000);
            using var stream = client.GetStream();

            string message = $"{username}|{password}";
            byte[] data = Encoding.UTF8.GetBytes(message);
            stream.Write(data, 0, data.Length);

            byte[] buffer = new byte[1024];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

            Console.WriteLine($"📩 Phản hồi từ server: {response}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Lỗi: {ex.Message}");
        }
    }
}