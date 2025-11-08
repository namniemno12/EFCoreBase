using MyProject.Domain.Entities.Implements;

namespace MyProject.Domain.Entities
{
    public class LoginHistory : Entity<Guid>
    {
        public Guid UserId { get; set; }               
        public DateTime LoginTime { get; set; } = DateTime.UtcNow;
        public DateTime? LogoutTime { get; set; }       
        public string? IpAddress { get; set; }
        public string? DeviceInfo { get; set; }        
        public bool IsSuccessful { get; set; }         
    }
}
