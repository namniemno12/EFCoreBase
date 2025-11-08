using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyProject.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("5f6f9c3c-a55e-4537-942d-3a9aabba88ba"),
                column: "CreatedAt",
                value: new DateTime(2025, 11, 8, 3, 6, 4, 159, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("dad41708-e38d-4674-b47b-012a26ec0274"),
                column: "CreatedAt",
                value: new DateTime(2025, 11, 8, 3, 6, 4, 159, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("0449a75f-afa0-45bf-939d-b78b12f832d6"),
                column: "CreatedAt",
                value: new DateTime(2025, 11, 8, 3, 6, 4, 160, DateTimeKind.Utc));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("5f6f9c3c-a55e-4537-942d-3a9aabba88ba"),
                column: "CreatedAt",
                value: new DateTime(2025, 11, 8, 3, 6, 4, 159, DateTimeKind.Utc).AddTicks(9653));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("dad41708-e38d-4674-b47b-012a26ec0274"),
                column: "CreatedAt",
                value: new DateTime(2025, 11, 8, 3, 6, 4, 159, DateTimeKind.Utc).AddTicks(9525));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("0449a75f-afa0-45bf-939d-b78b12f832d6"),
                column: "CreatedAt",
                value: new DateTime(2025, 11, 8, 3, 6, 4, 160, DateTimeKind.Utc).AddTicks(4201));
        }
    }
}
