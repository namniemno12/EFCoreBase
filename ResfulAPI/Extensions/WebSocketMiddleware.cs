using MyProject.Helper.Utils.Interfaces;
using ResfulAPI.Handler;
using System.Net.WebSockets;
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
                using var webSocket = await context.WebSockets.AcceptWebSocketAsync();

                var username = context.Request.Query["username"].ToString();
                var token = context.Request.Query["access_token"].ToString();
                var utilsServices = context.RequestServices.GetService<ITokenUtils>();

                Guid? userId = null;
                if (!string.IsNullOrEmpty(token))
                {
                    userId = utilsServices?.ValidateToken(token);
                }

                switch (context.Request.Path.Value)
                {
                    case "/ws/AdminGroup":
                        {
                            var adminHandler = new AdminWebSocketHandler(webSocket, _webSocketManager);
                            await adminHandler.HandleWebSocketAsync(userId?.ToString() ?? "Anonymous", "AdminGroup");
                            break;
                        }
                    case "/ws/UserConnection":
                        {
                            var userHandler = new UserWebSocketHandler(webSocket, _webSocketManager);
                            await userHandler.HandleWebSocketAsync(username ?? "Anonymous", "NotifyLogin");
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
            else
            {
                await _next(context);
            }
        }

    }
}