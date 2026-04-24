using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodStreetApp.CMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameUserIdToDeviceId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "UserHistories",
                newName: "DeviceId");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "CreatedAtUtc",
                value: new DateTime(2026, 4, 24, 6, 24, 17, 162, DateTimeKind.Utc).AddTicks(3739));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "CreatedAtUtc",
                value: new DateTime(2026, 4, 24, 6, 24, 17, 162, DateTimeKind.Utc).AddTicks(6437));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DeviceId",
                table: "UserHistories",
                newName: "UserId");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "CreatedAtUtc",
                value: new DateTime(2026, 4, 20, 13, 29, 18, 606, DateTimeKind.Utc).AddTicks(5090));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "CreatedAtUtc",
                value: new DateTime(2026, 4, 20, 13, 29, 18, 606, DateTimeKind.Utc).AddTicks(5937));
        }
    }
}
