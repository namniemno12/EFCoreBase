using MyProject.Application.Services.Interfaces;
using MyProject.Application.WebSockets.Interfaces;
using MyProject.Domain.DTOs.Auth.Req;
using MyProject.Domain.DTOs.Auth.Res;
using MyProject.Helper.Utils;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace MyProject.Application.TcpSocket
{
    public class TcpSocketServer
    {
        private readonly int _port;
        private readonly IWebSocketService _webSocketService;
        private readonly IAuthServices _authServices;
        public TcpSocketServer(int port, IWebSocketService webSocketService, IAuthServices authServices)
        {
            _port = port;
            _webSocketService = webSocketService;
            _authServices = authServices;
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

                try
                {
                    var loginMsg = System.Text.Json.JsonSerializer.Deserialize<CommonMessage<object>>(message);
                    if (loginMsg != null)
                    {
                        switch (loginMsg.Method)
                        {
                            case "LoginRequest":
                                var loginData = System.Text.Json.JsonSerializer.Deserialize<LoginDataReq>(
                                    loginMsg.Data.ToString() ?? "{}"
                                );

                                if (loginData != null)
                                {
                                    var req = new AddLoginRequestReq
                                    {
                                        UserId = loginData.UserId,
                                        IpAddress = loginData.IpAddress,
                                        DeviceInfo = loginData.DeviceInfo,
                                        Status = 0,
                                        RequestedAt = DateTime.UtcNow
                                    };

                                    var result = await _authServices.AddLoginRequest(req);
                                    await _webSocketService.NotifyAdminsUserLoggedInAsync(result.Data);
                                }
                                break;
                            case "AcceptLogin":
                                try
                                {
                                    var acceptLoginData = JsonSerializer.Deserialize<DataAcceptLoginRes>(
                                        loginMsg.Data.ToString() ?? "{}"
                                    );

                                    if (acceptLoginData != null)
                                    {
                                        await _webSocketService.NotifyUserByAdminAsync(acceptLoginData.UserName, acceptLoginData.Status);
                                        var loginHistoryReq = new AddLoginHistoryReq
                                        {
                                            UserId = acceptLoginData.UserId,
                                            LoginTime = DateTime.UtcNow,
                                            IpAddress = acceptLoginData.IpAddress,
                                            DeviceInfo = acceptLoginData.DeviceInfo,
                                            IsSuccessful = acceptLoginData.Status == 1 
                                        };
                                        await _authServices.AddLoginHistory(loginHistoryReq);

                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"⚠️ JSON parse error AcceptLogin: {ex.Message}");
                                }
                                break;
                            default:
                                break;
                        }
                    }
                }
                catch (Exception jsonEx)
                {
                }

                var response = Encoding.UTF8.GetBytes("✅ Message delivered to WebSocket clients");
                await stream.WriteAsync(response, 0, response.Length);
            }
            catch (Exception ex)
            {
            }
            finally
            {
                client.Close();
            }
        }
    }
}
