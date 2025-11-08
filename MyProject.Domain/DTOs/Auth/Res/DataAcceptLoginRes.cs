namespace MyProject.Domain.DTOs.Auth.Res
{
    public class DataAcceptLoginRes
    {
        public Guid LoginRequestId { get; set; }
        public Guid AdminId { get; set; }
        public Guid UserId { get; set; }
        public string? UserName { get; set; }
        public int Status { get; set; }
        public string? IpAddress { get; set; }
        public string? DeviceInfo { get; set; }
    }
}
