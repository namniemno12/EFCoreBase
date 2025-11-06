using System.Net.Sockets;
using System.Text;

namespace ResfulAPI.Services
{
 public interface ITcpClientService
    {
        Task<string> SendLoginRequest(string username, string password);
    }

    public class TcpClientService : ITcpClientService
    {
        private readonly string _serverIp;
        private readonly int _serverPort;

        public TcpClientService(IConfiguration configuration)
   {
            _serverIp = "127.0.0.1"; // Localhost
            _serverPort = 9000; // TCP Server port
   }

        public async Task<string> SendLoginRequest(string username, string password)
        {
            using (TcpClient client = new TcpClient())
      {
         await client.ConnectAsync(_serverIp, _serverPort);
  using (NetworkStream stream = client.GetStream())
      {
 // Format message as per TCP server requirements: username|password
             string message = $"{username}|{password}";
      byte[] data = Encoding.UTF8.GetBytes(message);
  await stream.WriteAsync(data, 0, data.Length);

       // Read response
         byte[] buffer = new byte[1024];
         int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
           string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
        return response;
           }
    }
     }
    }
}