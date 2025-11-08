namespace MyProject.Domain.DTOs.Auth.Req
{
    public class LoginCmsRequest
    {
        public string? UserName { get; set; }
        public string? Password { get; set; }
    }

    public class UserLoginResponse
    {
        public string? access_token { get; set; }
        public string? refresh_token { get; set; }
        public string? session_token { get; set; }
        public long UserID { get; set; }
        public bool isWalletConnected { get; set; }
    }

    public class LoginTypeDTO
    {
        public long AdminID { get; set; }
        public string UserName { get; set; }
        public string DisplayName { get; set; }
    }

    public class UserConnectWalletResponse
    {
        public string ReferenceId { get; set; }
        public string Email { get; set; }
        public string ExternalWalletAddress { get; set; }

    }
    public class LoginCmsResponse
    {
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
    }
    public class LoginCmsDTO
    {
        public long ID { get; set; }
        public string UserName { get; set; }
        public string DisplayName { get; set; }
    }
}
