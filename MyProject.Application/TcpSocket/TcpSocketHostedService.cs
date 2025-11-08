using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MyProject.Application.TcpSocket
{
    public class TcpSocketHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public TcpSocketHostedService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var tcpServer = scope.ServiceProvider.GetRequiredService<TcpSocketServer>();
            await tcpServer.StartAsync(stoppingToken);
        }
    }
}
