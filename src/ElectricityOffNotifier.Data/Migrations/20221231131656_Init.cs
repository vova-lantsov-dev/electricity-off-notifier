using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ElectricityOffNotifier.Data.Migrations
{
    public partial class Init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.CreateTable(
                name: "cities",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    region = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cities", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "addresses",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    street = table.Column<string>(type: "text", nullable: false),
                    building_no = table.Column<string>(type: "text", nullable: false),
                    city_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_addresses", x => x.id);
                    table.ForeignKey(
                        name: "FK_addresses_cities_city_id",
                        column: x => x.city_id,
                        principalSchema: "public",
                        principalTable: "cities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "checkers",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    address_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_checkers", x => x.id);
                    table.ForeignKey(
                        name: "FK_checkers_addresses_address_id",
                        column: x => x.address_id,
                        principalSchema: "public",
                        principalTable: "addresses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "checker_entries",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    checker_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_checker_entries", x => x.id);
                    table.ForeignKey(
                        name: "FK_checker_entries_checkers_checker_id",
                        column: x => x.checker_id,
                        principalSchema: "public",
                        principalTable: "checkers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "producers",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    access_token_hash = table.Column<byte[]>(type: "bytea", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    checker_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_producers", x => x.id);
                    table.ForeignKey(
                        name: "FK_producers_checkers_checker_id",
                        column: x => x.checker_id,
                        principalSchema: "public",
                        principalTable: "checkers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sent_notifications",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_up_notification = table.Column<bool>(type: "boolean", nullable: false),
                    checker_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sent_notifications", x => x.id);
                    table.ForeignKey(
                        name: "FK_sent_notifications_checkers_checker_id",
                        column: x => x.checker_id,
                        principalSchema: "public",
                        principalTable: "checkers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "subscribers",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    telegram_id = table.Column<long>(type: "bigint", nullable: false),
                    checker_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscribers", x => x.id);
                    table.ForeignKey(
                        name: "FK_subscribers_checkers_checker_id",
                        column: x => x.checker_id,
                        principalSchema: "public",
                        principalTable: "checkers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_addresses_city_id",
                schema: "public",
                table: "addresses",
                column: "city_id");

            migrationBuilder.CreateIndex(
                name: "IX_checker_entries_checker_id",
                schema: "public",
                table: "checker_entries",
                column: "checker_id");

            migrationBuilder.CreateIndex(
                name: "IX_checkers_address_id",
                schema: "public",
                table: "checkers",
                column: "address_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_producers_checker_id",
                schema: "public",
                table: "producers",
                column: "checker_id");

            migrationBuilder.CreateIndex(
                name: "IX_sent_notifications_checker_id",
                schema: "public",
                table: "sent_notifications",
                column: "checker_id");

            migrationBuilder.CreateIndex(
                name: "IX_subscribers_checker_id",
                schema: "public",
                table: "subscribers",
                column: "checker_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "checker_entries",
                schema: "public");

            migrationBuilder.DropTable(
                name: "producers",
                schema: "public");

            migrationBuilder.DropTable(
                name: "sent_notifications",
                schema: "public");

            migrationBuilder.DropTable(
                name: "subscribers",
                schema: "public");

            migrationBuilder.DropTable(
                name: "checkers",
                schema: "public");

            migrationBuilder.DropTable(
                name: "addresses",
                schema: "public");

            migrationBuilder.DropTable(
                name: "cities",
                schema: "public");
        }
    }
}
