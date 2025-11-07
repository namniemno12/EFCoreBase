namespace MyProject.Application.WebSockets.Interfaces
{
    public interface IWebSocketService
    {
        Task NotifyAdminsUserLoggedInAsync(string message);
        Task NotifyUserByAdminAsync(string userId, string message);
    }
}
