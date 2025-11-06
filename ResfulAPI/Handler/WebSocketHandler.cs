using Newtonsoft.Json;
using System.Net.WebSockets;
using System.Text;
using Microsoft.Extensions.Logging;

namespace ResfulAPI.Handler
{
    public class WebSocketHandler
    {
        private readonly WebSocket _webSocket;
 private readonly ILogger _logger;
  private readonly CancellationTokenSource _cancellationTokenSource;
        private const int ReceiveBufferSize = 4 * 1024;
 private static readonly List<WebSocket> _connectedSockets = new();
    private static readonly object _lockObject = new();

        public WebSocketHandler(WebSocket webSocket, ILogger logger)
  {
            _webSocket = webSocket;
    _logger = logger;
     _cancellationTokenSource = new CancellationTokenSource();

  lock (_lockObject)
            {
       _connectedSockets.Add(webSocket);
  }
      }

        public virtual async Task HandleConnection()
        {
            var buffer = new byte[ReceiveBufferSize];

     try
          {
             while (_webSocket.State == WebSocketState.Open)
    {
     var result = await _webSocket.ReceiveAsync(
    new ArraySegment<byte>(buffer),
       _cancellationTokenSource.Token);

       if (result.MessageType == WebSocketMessageType.Close)
   {
    await HandleCloseMessage();
  break;
}

        if (result.MessageType == WebSocketMessageType.Text)
      {
      var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
      await HandleMessage(message);
  }
         }
            }
   catch (Exception ex)
       {
        _logger.LogError(ex, "Error in WebSocket connection");
  }
finally
            {
     await CleanupAsync();
        }
     }

        private async Task HandleMessage(string message)
        {
     try
 {
       _logger.LogInformation("Received message: {Message}", message);
      var messageObj = JsonConvert.DeserializeObject<CommonMessage<dynamic>>(message);

 // Handle TCP Server messages
                if (messageObj.Method == "user_login_success")
{
               await BroadcastMessage(Encoding.UTF8.GetBytes(message));
                }
            }
            catch (Exception ex)
            {
             _logger.LogError(ex, "Error processing message");
 }
        }

        public static async Task BroadcastMessage(ArraySegment<byte> message)
        {
    List<WebSocket> deadSockets = new();

            lock (_lockObject)
  {
foreach (var socket in _connectedSockets)
  {
      try
    {
       if (socket.State == WebSocketState.Open)
    {
                 _ = socket.SendAsync(
      message,
         WebSocketMessageType.Text,
            true,
           CancellationToken.None);
   }
     else
     {
 deadSockets.Add(socket);
           }
        }
         catch
             {
            deadSockets.Add(socket);
       }
       }

            // Cleanup dead sockets
   foreach (var socket in deadSockets)
      {
      _connectedSockets.Remove(socket);
             }
      }
        }

   private async Task HandleCloseMessage()
        {
            if (_webSocket.State == WebSocketState.Open)
        {
        await _webSocket.CloseAsync(
            WebSocketCloseStatus.NormalClosure,
 "Connection closed",
      CancellationToken.None);
     }
        }

        private async Task CleanupAsync()
        {
      lock (_lockObject)
   {
    _connectedSockets.Remove(_webSocket);
  }

if (_webSocket.State == WebSocketState.Open)
    {
      try
          {
       await _webSocket.CloseAsync(
            WebSocketCloseStatus.NormalClosure,
         "Cleanup",
       CancellationToken.None);
           }
          catch (Exception ex)
          {
      _logger.LogError(ex, "Error during cleanup");
             }
 }
        }
    }

    public class CommonMessage<T>
    {
        [JsonProperty("i")]
      public long MessageId { get; set; }

        [JsonProperty("m")]
        public string Method { get; set; }

        [JsonProperty("dt")]
        public T Data { get; set; }
    }
}