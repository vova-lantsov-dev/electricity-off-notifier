using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectricityOffNotifier.Data.Migrations
{
    public partial class OneToManyChatInfoSubscribers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_subscribers_telegram_id",
                schema: "public",
                table: "subscribers");

            migrationBuilder.CreateIndex(
                name: "IX_subscribers_telegram_id",
                schema: "public",
                table: "subscribers",
                column: "telegram_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_subscribers_telegram_id",
                schema: "public",
                table: "subscribers");

            migrationBuilder.CreateIndex(
                name: "IX_subscribers_telegram_id",
                schema: "public",
                table: "subscribers",
                column: "telegram_id",
                unique: true);
        }
    }
}
