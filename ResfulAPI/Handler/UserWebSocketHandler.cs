using System.Net.WebSockets;

namespace ResfulAPI.Handler
{
    public class UserWebSocketHandler
    {
        private readonly WebSocket _webSocket;
        private readonly IWebSocketManager _webSocketManager;
        public UserWebSocketHandler(WebSocket webSocket, IWebSocketManager webSocketManager)
        {
            _webSocket = webSocket;
            _webSocketManager = webSocketManager;
        }
        public async Task HandleWebSocketAsync(long playerId, string hub)
        {
            await _webSocketManager.AddUserSocketAsync(hub, playerId.ToString(), _webSocket);

            try
            {
                var buffer = new byte[1024 * 4];
                while (_webSocket.State == WebSocketState.Open)
                {
                    var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Close)
                        break;
                }
            }
            finally
            {
                await _webSocketManager.RemoveUserSocketAsync(hub, playerId.ToString());
                await _webSocketManager.HandleDisconnectAsync(_webSocket);
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            }
        }
    }
}
