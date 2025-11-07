using MyProject.Application.WebSockets.Interfaces;
using System.Text.Json;

public class LoginService
{
    private readonly IWebSocketService _webSocketService;

    public LoginService(IWebSocketService webSocketService)
    {
        _webSocketService = webSocketService;
    }

    public async Task<string> HandleLogin(string message)
    {
        try
        {
            var userInfo = JsonSerializer.Deserialize<UserInfo>(message);
            if (userInfo != null && !string.IsNullOrEmpty(userInfo.username))
            {
                await _webSocketService.NotifyAdminsUserLoggedInAsync($"{userInfo.username} vừa đăng nhập!");
                return $"success|Xin chào {userInfo.username}!";
            }
            else
            {
                return "failed|Thiếu thông tin username!";
            }
        }
        catch
        {
            return "failed|Dữ liệu không đúng định dạng JSON!";
        }
    }

    public class UserInfo
    {
        public string username { get; set; }
        public string password { get; set; }
        public string email { get; set; }
        public int age { get; set; }
    }
}
