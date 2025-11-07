using Microsoft.Extensions.Hosting;

namespace MyProject.Application.TcpSocket
{
    public class TcpSocketHostedService : BackgroundService
    {
        private readonly TcpSocketServer _tcpServer;

        public TcpSocketHostedService(TcpSocketServer tcpServer)
        {
            _tcpServer = tcpServer;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _tcpServer.StartAsync(stoppingToken);
        }
    }
}
