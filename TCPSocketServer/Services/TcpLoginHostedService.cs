using Microsoft.Extensions.Hosting;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class TcpLoginHostedService : BackgroundService
{
    private readonly LoginService _loginService;

    public TcpLoginHostedService(LoginService loginService)
    {
        _loginService = loginService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var server = new TcpListener(IPAddress.Any, 9000);
        server.Start();
        Console.WriteLine("✅ TCP Login Server đang chạy tại cổng9000...\n");

        while (!stoppingToken.IsCancellationRequested)
        {
            var client = await server.AcceptTcpClientAsync(stoppingToken);
            _ = HandleClientAsync(client, stoppingToken);
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        using var stream = client.GetStream();
        byte[] buffer = new byte[1024];
        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

        string response = await _loginService.HandleLogin(message);

        byte[] data = Encoding.UTF8.GetBytes(response);
        await stream.WriteAsync(data, 0, data.Length, cancellationToken);

        client.Close();
    }
}
