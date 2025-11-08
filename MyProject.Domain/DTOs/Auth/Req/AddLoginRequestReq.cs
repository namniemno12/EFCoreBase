namespace MyProject.Domain.DTOs.Auth.Req
{
    public class AddLoginRequestReq
    {
        public Guid UserId { get; set; }
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public int Status { get; set; }
        public string? IpAddress { get; set; }
        public string? DeviceInfo { get; set; }
        public string? AdminNote { get; set; }
        public Guid? ReviewedByAdminId { get; set; }
        public Guid? LoginHistoryId { get; set; }
    }
}
