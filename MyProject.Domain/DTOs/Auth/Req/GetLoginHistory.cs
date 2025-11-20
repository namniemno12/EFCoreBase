namespace MyProject.Domain.DTOs.Auth.Req
{
    public class GetLoginHistory
    {
        public Guid LoginHistoryId { get; set; }
        public Guid UserId { get; set; }
        public string? UserName { get; set; }
        public string? FullName { get; set; }
        public DateTime LoginTime { get; set; }
        public string? IpAddress { get; set; }
        public string? DeviceInfo { get; set; }
        public bool IsSuccessful { get; set; }
    }
}
