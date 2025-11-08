
using MyProject.Domain.Entities.Implements;

namespace MyProject.Domain.Entities
{
    public class Users : Entity<Guid>
    {
        public string UserName { get; set; } = null!;   
        public string Email { get; set; } = null!;
        public string PasswordHash { get; set; } = null!; 
        public string? FullName { get; set; }          
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public bool IsActive { get; set; } = true;      
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
        public Guid RoleId { get; set; }              

    }
}
