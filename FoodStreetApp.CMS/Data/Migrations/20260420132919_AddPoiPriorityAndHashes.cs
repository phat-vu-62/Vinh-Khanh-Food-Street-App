using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FoodStreetApp.CMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPoiPriorityAndHashes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Amount",
                table: "UserHistories",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                table: "Pois",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerId",
                table: "Pois",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "Pois",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Username = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    FullName = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Address = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Address", "CreatedAtUtc", "Email", "FullName", "IsActive", "PasswordHash", "PhoneNumber", "Role", "Username" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000001"), null, new DateTime(2026, 4, 20, 13, 29, 18, 606, DateTimeKind.Utc).AddTicks(5090), null, null, true, "$2a$11$q9hM6lJOPU1VwL6P3wF64OgQGqR.k54bF.Q4.Nq3C05tD/tG0G4Q.", null, "Admin", "admin" },
                    { new Guid("00000000-0000-0000-0000-000000000002"), null, new DateTime(2026, 4, 20, 13, 29, 18, 606, DateTimeKind.Utc).AddTicks(5937), null, null, true, "$2a$11$q9hM6lJOPU1VwL6P3wF64OgQGqR.k54bF.Q4.Nq3C05tD/tG0G4Q.", null, "Owner", "owner" }
                });

            migrationBuilder.AddForeignKey(
                name: "FK_PoiSyncActions_Pois_PoiId",
                table: "PoiSyncActions",
                column: "PoiId",
                principalTable: "Pois",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PoiSyncActions_Pois_PoiId",
                table: "PoiSyncActions");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropColumn(
                name: "Amount",
                table: "UserHistories");

            migrationBuilder.DropColumn(
                name: "IsApproved",
                table: "Pois");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Pois");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "Pois");
        }
    }
}
