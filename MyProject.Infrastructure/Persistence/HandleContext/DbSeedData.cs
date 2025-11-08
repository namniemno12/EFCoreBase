using Microsoft.EntityFrameworkCore;
using MyProject.Domain.Entities;

namespace MyProject.Infrastructure.Persistence.HandleContext
{
    public static class DbSeedData
    {
        public static void Seed(ModelBuilder modelBuilder)
        {
            // Sử dụng giá trị cứng cho seed data để tránh lỗi migration
            var adminRoleId = new Guid("dad41708-e38d-4674-b47b-012a26ec0274");
            var userRoleId = new Guid("5f6f9c3c-a55e-4537-942d-3a9aabba88ba");
            var adminUserId = new Guid("0449a75f-afa0-45bf-939d-b78b12f832d6");
            var createdAtRole = new DateTime(2025, 11, 8, 3, 6, 4, 159, DateTimeKind.Utc);
            var createdAtUser = new DateTime(2025, 11, 8, 3, 6, 4, 160, DateTimeKind.Utc);

            modelBuilder.Entity<Roles>().HasData(
                new Roles { Id = adminRoleId, Name = "Admin", CreatedAt = createdAtRole },
                new Roles { Id = userRoleId, Name = "User", CreatedAt = createdAtRole }
            );

            modelBuilder.Entity<Users>().HasData(
                new Users
                {
                    Id = adminUserId,
                    UserName = "admin",
                    Email = "admin@yourdomain.com",
                    PasswordHash = "o5_OeffS5AwBG7EycB6RCrWfKbB0pRvSoEA13EXo5a4", // Giá trị hash cứng
                    FullName = "Administrator",
                    IsActive = true,
                    CreatedAt = createdAtUser,
                    RoleId = adminRoleId
                }
            );
        }
    }
}
