namespace MyProject.Domain.DTOs.Auth.Req
{
    public class LoginDataReq
    {
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? IpAddress { get; set; }
        public string? DeviceInfo { get; set; }
    }
}
