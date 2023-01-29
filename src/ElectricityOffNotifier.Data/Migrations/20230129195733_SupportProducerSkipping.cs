using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectricityOffNotifier.Data.Migrations
{
    public partial class SupportProducerSkipping : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "skipped_until",
                schema: "public",
                table: "producers",
                type: "timestamp",
                nullable: false,
                defaultValue: new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "skipped_until",
                schema: "public",
                table: "producers");
        }
    }
}
