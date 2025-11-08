namespace MyProject.Domain.DTOs.Auth.Res
{
    public class GetLoginRequestRes
    {
        public Guid LoginRequestId { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; }
        public string FullName { get; set; }
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReviewedAt { get; set; }
        public int Status { get; set; }
        public string? IpAddress { get; set; }
        public string? DeviceInfo { get; set; }
        public Guid? ReviewedByAdminId { get; set; }
        public Guid? LoginHistoryId { get; set; }
    }
}
