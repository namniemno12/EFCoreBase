using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyProject.Application.WebSockets;
using MyProject.Application.WebSockets.Interfaces;
public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                services.AddSingleton<IWebSocketService, WebSocketService>();
                services.AddSingleton<LoginService>();
                services.AddHostedService<TcpLoginHostedService>();
            });
        await builder.Build().RunAsync();
    }
}