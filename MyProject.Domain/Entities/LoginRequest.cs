using MyProject.Domain.Entities.Implements;

namespace MyProject.Domain.Entities
{
    public class LoginRequest : Entity<Guid>
    {
        public Guid UserId { get; set; }
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow; 
        public DateTime? ReviewedAt { get; set; }                    
        public int Status { get; set; } 
        public string? IpAddress { get; set; }
        public string? DeviceInfo { get; set; }
        public string? AdminNote { get; set; }                      
        public Guid? ReviewedByAdminId { get; set; }                  

        public Guid? LoginHistoryId { get; set; }
    }
}
