using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectricityOffNotifier.Data.Migrations
{
    public partial class SubscriberProducerBind : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "producer_id",
                schema: "public",
                table: "subscribers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_subscribers_producer_id",
                schema: "public",
                table: "subscribers",
                column: "producer_id");

            migrationBuilder.AddForeignKey(
                name: "FK_subscribers_producers_producer_id",
                schema: "public",
                table: "subscribers",
                column: "producer_id",
                principalSchema: "public",
                principalTable: "producers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_subscribers_producers_producer_id",
                schema: "public",
                table: "subscribers");

            migrationBuilder.DropIndex(
                name: "IX_subscribers_producer_id",
                schema: "public",
                table: "subscribers");

            migrationBuilder.DropColumn(
                name: "producer_id",
                schema: "public",
                table: "subscribers");
        }
    }
}
