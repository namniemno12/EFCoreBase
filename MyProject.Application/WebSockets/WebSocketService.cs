using MyProject.Application.WebSockets.Interfaces;
using MyProject.Helper.Utils;
using Newtonsoft.Json;

namespace MyProject.Application.WebSockets
{
    public class WebSocketService : IWebSocketService
    {
        private readonly IWebSocketManager _webSocketManager;

        public WebSocketService(IWebSocketManager webSocketManager)
        {
            _webSocketManager = webSocketManager;
        }

        public async Task NotifyAdminsUserLoggedInAsync(string message)
        {
            var messageSend = new CommonMessage<dynamic>
            {
                MessageId = 1,
                Method = "NotifyNewUserLogin",
                Data = message
            };
            var json = JsonConvert.SerializeObject(messageSend);
            await _webSocketManager.SendMessageToGroupAsync("AdminGroup", json);
        }

        public async Task NotifyUserByAdminAsync(string userId, string message)
        {
            var messageSend = new CommonMessage<dynamic>
            {
                MessageId = 1,
                Method = "NotifyLogin",
                Data = message
            };
            var json = JsonConvert.SerializeObject(messageSend);
            await _webSocketManager.SendMessageToUserAsync("NotifyLogin", userId, json);
        }
    }
}
