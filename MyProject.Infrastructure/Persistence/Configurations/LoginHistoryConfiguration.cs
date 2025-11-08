using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyProject.Domain.Entities;

namespace MyProject.Infrastructure.Persistence.Configurations
{
    public class LoginHistoryConfiguration : IEntityTypeConfiguration<LoginHistory>
    {
        public void Configure(EntityTypeBuilder<LoginHistory> builder)
        {
            builder.HasKey(l => l.Id);
            builder.Property(l => l.Id).ValueGeneratedOnAdd();
            builder.Property(l => l.UserId).IsRequired();
            builder.Property(l => l.LoginTime).IsRequired();
            builder.Property(l => l.LogoutTime);
            builder.Property(l => l.IpAddress).HasMaxLength(50);
            builder.Property(l => l.DeviceInfo).HasMaxLength(255);
            builder.Property(l => l.IsSuccessful).IsRequired();
        }
    }
}
