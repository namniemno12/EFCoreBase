namespace MyProject.Domain.DTOs.Auth.Req
{
    public class AddLoginHistoryReq
    {
        public Guid UserId { get; set; }
        public DateTime LoginTime { get; set; } = DateTime.UtcNow;
        public string? IpAddress { get; set; }
        public string? DeviceInfo { get; set; }
        public bool IsSuccessful { get; set; }
    }
}
