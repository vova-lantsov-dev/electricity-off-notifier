using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectricityOffNotifier.Data.Migrations
{
    public partial class TelegramMessageThreadId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "telegram_thread_id",
                schema: "public",
                table: "subscribers",
                type: "integer",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "telegram_thread_id",
                schema: "public",
                table: "subscribers");
        }
    }
}
