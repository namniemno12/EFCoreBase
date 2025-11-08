using MyProject.Domain.DTOs.Auth.Res;

namespace MyProject.Application.WebSockets.Interfaces
{
    public interface IWebSocketService
    {
        Task NotifyAdminsUserLoggedInAsync(GetLoginRequestRes req);
        Task NotifyUserByAdminAsync(string userId, int Status);
    }
}
