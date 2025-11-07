using System.Net.WebSockets;
using ResfulAPI.Handler;
namespace ResfulAPI.Extensions
{
    public class WebSocketMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IWebSocketManager _webSocketManager;
        public WebSocketMiddleware(RequestDelegate next, IWebSocketManager webSocketManager)
        {
            _next = next;
            _webSocketManager = webSocketManager;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                try
                {
                    using var webSocket = await context.WebSockets.AcceptWebSocketAsync();

                    switch (context.Request.Path.Value)
                    {
                        case "/ws/AdminGroup":
                            {
                                var adminHandler = new AdminWebSocketHandler(webSocket, _webSocketManager);
                                await adminHandler.HandleWebSocketAsync(123, "AdminGroup");
                                break;
                            }
                        case "/ws/UserConnection":
                            {
                                var userHandler = new UserWebSocketHandler(webSocket, _webSocketManager);
                                await userHandler.HandleWebSocketAsync(123, "NotifyLogin");
                                break;
                            }
                        default:
                            if (webSocket.State == WebSocketState.Open)
                            {
                                await webSocket.CloseAsync(
                                    WebSocketCloseStatus.InvalidPayloadData,
                                    "Invalid path",
                                    CancellationToken.None);
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                }
            }
            else
            {
                await _next(context);
            }
        }

    }
}