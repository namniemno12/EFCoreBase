using MyProject.Domain.Entities.Implements;

namespace MyProject.Domain.Entities
{
    public class Roles :  Entity<Guid>
    {
        public string Name { get; set; } = null!;     
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
