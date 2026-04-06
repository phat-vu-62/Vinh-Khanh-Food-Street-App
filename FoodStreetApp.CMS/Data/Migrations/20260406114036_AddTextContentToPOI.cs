using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodStreetApp.CMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTextContentToPOI : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TextContent",
                table: "Pois",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TextContentEn",
                table: "Pois",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TextContentJa",
                table: "Pois",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TextContentKo",
                table: "Pois",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TextContentVi",
                table: "Pois",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TextContentZh",
                table: "Pois",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TextContent",
                table: "Pois");

            migrationBuilder.DropColumn(
                name: "TextContentEn",
                table: "Pois");

            migrationBuilder.DropColumn(
                name: "TextContentJa",
                table: "Pois");

            migrationBuilder.DropColumn(
                name: "TextContentKo",
                table: "Pois");

            migrationBuilder.DropColumn(
                name: "TextContentVi",
                table: "Pois");

            migrationBuilder.DropColumn(
                name: "TextContentZh",
                table: "Pois");
        }
    }
}
