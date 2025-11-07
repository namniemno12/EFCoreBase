using MyProject.Application.TcpSocket.Interfaces;
using System.Net.Sockets;
using System.Text;

namespace MyProject.Application.TcpSocket
{
    public class TcpSocketService : ITcpSocketService
    {
        private readonly string _host;
        private readonly int _port;
        public TcpSocketService(string host = "127.0.0.1", int port = 9000)
        {
            _host = host;
            _port = port;
        }

        public async Task<string> SendMessageAsync(string message)
        {
            using var client = new TcpClient();
            await client.ConnectAsync(_host, _port);

            var stream = client.GetStream();
            var data = Encoding.UTF8.GetBytes(message);
            await stream.WriteAsync(data, 0, data.Length);

            var buffer = new byte[4096];
            int bytesRead = await stream.ReadAsync(buffer);
            return Encoding.UTF8.GetString(buffer, 0, bytesRead);
        }
    }
}
