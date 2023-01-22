using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectricityOffNotifier.Data.Migrations
{
    public partial class ChatInfoWithMsgTemplates : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "chat_info",
                schema: "public",
                columns: table => new
                {
                    telegram_id = table.Column<long>(type: "bigint", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false, defaultValue: "NAME"),
                    message_up_template = table.Column<string>(type: "text", nullable: false, defaultValue: "Повідомлення за адресою <b>{{Address}}</b>:\n\n<b>Електропостачання відновлено!</b>\n{{#SinceRegion}}\nБуло відсутнє з {{SinceDate}}\nЗагальна тривалість відключення: {{DurationHours}} год. {{DurationMinutes}} хв.\n{{/SinceRegion}}"),
                    message_down_template = table.Column<string>(type: "text", nullable: false, defaultValue: "Повідомлення за адресою <b>{{Address}}</b>:\n\n<b>Електропостачання відсутнє!</b>\n{{#SinceRegion}}\nЧас початку відключення: {{SinceDate}}\nСвітло було протягом {{DurationHours}} год. {{DurationMinutes}} хв.\n{{/SinceRegion}}")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_info", x => x.telegram_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_subscribers_telegram_id",
                schema: "public",
                table: "subscribers",
                column: "telegram_id",
                unique: true);

            migrationBuilder.Sql(
                "INSERT INTO public.chat_info(telegram_id) SELECT telegram_id FROM public.subscribers;");

            migrationBuilder.AddForeignKey(
                name: "FK_subscribers_chat_info_telegram_id",
                schema: "public",
                table: "subscribers",
                column: "telegram_id",
                principalSchema: "public",
                principalTable: "chat_info",
                principalColumn: "telegram_id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_subscribers_chat_info_telegram_id",
                schema: "public",
                table: "subscribers");

            migrationBuilder.DropTable(
                name: "chat_info",
                schema: "public");

            migrationBuilder.DropIndex(
                name: "IX_subscribers_telegram_id",
                schema: "public",
                table: "subscribers");
        }
    }
}
