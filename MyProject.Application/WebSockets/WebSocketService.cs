using MyProject.Application.WebSockets.Interfaces;
using MyProject.Domain.DTOs.Auth.Res;
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

        public async Task NotifyAdminsUserLoggedInAsync(GetLoginRequestRes req)
        {
            var messageSend = new CommonMessage<dynamic>
            {
                MessageId = 1,
                Method = "NotifyNewUserLogin",
                Data = req
            };
            var json = JsonConvert.SerializeObject(messageSend);
            await _webSocketManager.SendMessageToGroupAsync("AdminGroup", json);
        }

        public async Task NotifyUserByAdminAsync(string userId, int status)
        {
            var messageSend = new CommonMessage<dynamic>
            {
                MessageId = 1,
                Method = "NotifyLogin",
                Data = new
                {
                    IsSuccessful = status == 1 ? true : false
                }
            };

            var json = JsonConvert.SerializeObject(messageSend);
            await _webSocketManager.SendMessageToUserAsync("NotifyLogin", userId, json);
        }
    }
}
