using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectricityOffNotifier.Data.Migrations
{
    public partial class StoreTimeZoneAndCultureForSubscriber : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "culture",
                schema: "public",
                table: "subscribers",
                type: "text",
                nullable: false,
                defaultValue: "uk-UA");

            migrationBuilder.AddColumn<string>(
                name: "time_zone",
                schema: "public",
                table: "subscribers",
                type: "text",
                nullable: false,
                defaultValue: "Europe/Kiev");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "culture",
                schema: "public",
                table: "subscribers");

            migrationBuilder.DropColumn(
                name: "time_zone",
                schema: "public",
                table: "subscribers");
        }
    }
}
