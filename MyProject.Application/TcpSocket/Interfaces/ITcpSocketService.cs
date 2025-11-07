namespace MyProject.Application.TcpSocket.Interfaces
{
    public interface ITcpSocketService
    {
        Task<string> SendMessageAsync(string message);
    }
}
