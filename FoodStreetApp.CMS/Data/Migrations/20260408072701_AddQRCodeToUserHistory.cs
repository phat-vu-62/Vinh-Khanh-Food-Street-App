using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodStreetApp.CMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddQRCodeToUserHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "QRCode",
                table: "UserHistories",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Pois",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QRCode",
                table: "UserHistories");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Pois");
        }
    }
}
