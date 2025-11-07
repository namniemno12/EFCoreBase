using MyProject.Application.WebSockets.Interfaces;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MyProject.Application.TcpSocket
{
    public class TcpSocketServer
    {
        private readonly int _port;
        private readonly IWebSocketService _webSocketService;

        public TcpSocketServer(int port, IWebSocketService webSocketService)
        {
            _port = port;
            _webSocketService = webSocketService;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var listener = new TcpListener(IPAddress.Any, _port);
            listener.Start();
            Console.WriteLine($"✅ TCP Socket Server started on port {_port}");

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var client = await listener.AcceptTcpClientAsync(cancellationToken);
                    _ = HandleClientAsync(client);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ TCP Accept error: {ex.Message}");
                }
            }

            listener.Stop();
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            try
            {
                using var stream = client.GetStream();
                var buffer = new byte[1024];
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

                if (bytesRead <= 0 || bytesRead > buffer.Length)
                    return;

                var message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine($"📩 TCP received: {message}");

                await _webSocketService.NotifyAdminsUserLoggedInAsync(message);

                var response = Encoding.UTF8.GetBytes("✅ Message delivered to WebSocket clients");
                await stream.WriteAsync(response, 0, response.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ TCP HandleClient error: {ex.Message}");
            }
            finally
            {
                client.Close();
            }
        }
    }
}
