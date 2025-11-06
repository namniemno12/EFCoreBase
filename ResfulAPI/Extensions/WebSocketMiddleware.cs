using System.Net.WebSockets;
using ResfulAPI.Handler;
using Microsoft.Extensions.Logging;

namespace ResfulAPI.Extensions
{
    public class WebSocketMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<WebSocketMiddleware> _logger;

        public WebSocketMiddleware(RequestDelegate next, ILogger<WebSocketMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                try
                {
                    using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                    _logger.LogInformation("WebSocket connection established for path: {Path}", context.Request.Path);

                    switch (context.Request.Path)
                    {
                        case "/ws/test":
                            var handler = new WebSocketHandler(webSocket, _logger);
                            await handler.HandleConnection();
                            break;
                        default:
                            _logger.LogWarning("Unknown WebSocket path requested: {Path}", context.Request.Path);
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
                    _logger.LogError(ex, "Error handling WebSocket connection: {Error}", ex.Message);
                }
            }
            else
            {
                await _next(context);
            }
        }

        public static async Task BroadcastMessage(string message)
        {
            try
            {
                var buffer = System.Text.Encoding.UTF8.GetBytes(message);
                var segment = new ArraySegment<byte>(buffer);

                // Let the WebSocketHandler handle broadcasting
                await WebSocketHandler.BroadcastMessage(segment);
            }
            catch (Exception ex)
            {
                // Log error but don't throw
            }
        }
    }
}