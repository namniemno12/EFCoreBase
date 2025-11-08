namespace MyProject.Domain.DTOs.Auth.Req
{
    public class LoginDataReq
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; }
        public string? IpAddress { get; set; }
        public string? DeviceInfo { get; set; }
    }
}
