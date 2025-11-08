using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MyProject.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class seedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "CreatedAt", "Description", "Name" },
                values: new object[,]
                {
                    { new Guid("5f6f9c3c-a55e-4537-942d-3a9aabba88ba"), new DateTime(2025, 11, 8, 3, 6, 4, 159, DateTimeKind.Utc).AddTicks(9653), null, "User" },
                    { new Guid("dad41708-e38d-4674-b47b-012a26ec0274"), new DateTime(2025, 11, 8, 3, 6, 4, 159, DateTimeKind.Utc).AddTicks(9525), null, "Admin" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Address", "CreatedAt", "Email", "FullName", "IsActive", "LastLoginAt", "PasswordHash", "PhoneNumber", "RoleId", "UserName" },
                values: new object[] { new Guid("0449a75f-afa0-45bf-939d-b78b12f832d6"), null, new DateTime(2025, 11, 8, 3, 6, 4, 160, DateTimeKind.Utc).AddTicks(4201), "admin@yourdomain.com", "Administrator", true, null, "o5_OeffS5AwBG7EycB6RCrWfKbB0pRvSoEA13EXo5a4", null, new Guid("dad41708-e38d-4674-b47b-012a26ec0274"), "admin" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("5f6f9c3c-a55e-4537-942d-3a9aabba88ba"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("dad41708-e38d-4674-b47b-012a26ec0274"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("0449a75f-afa0-45bf-939d-b78b12f832d6"));
        }
    }
}
