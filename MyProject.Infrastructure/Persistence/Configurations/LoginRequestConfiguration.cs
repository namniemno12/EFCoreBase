using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyProject.Domain.Entities;

namespace MyProject.Infrastructure.Persistence.Configurations
{
    public class LoginRequestConfiguration : IEntityTypeConfiguration<LoginRequest>
    {
        public void Configure(EntityTypeBuilder<LoginRequest> builder)
        {
            builder.HasKey(lr => lr.Id);
            builder.Property(lr => lr.Id).ValueGeneratedOnAdd();
            builder.Property(lr => lr.UserId).IsRequired();
            builder.Property(lr => lr.RequestedAt).IsRequired();
            builder.Property(lr => lr.ReviewedAt);
            builder.Property(lr => lr.Status);
            builder.Property(lr => lr.IpAddress).HasMaxLength(50);
            builder.Property(lr => lr.DeviceInfo).HasMaxLength(255);
            builder.Property(lr => lr.AdminNote).HasMaxLength(255);
            builder.Property(lr => lr.ReviewedByAdminId);
            builder.Property(lr => lr.LoginHistoryId);
        }
    }
}
