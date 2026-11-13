using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaveBite.Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDealRecipientLocationGeoCursor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "address_version",
                table: "user_addresses",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<DateTime>(
                name: "feed_published_at",
                table: "flash_deals",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "publication_id",
                table: "flash_deals",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<long>(
                name: "visibility_version",
                table: "flash_deals",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.CreateTable(
                name: "deal_notification_recipients",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    publication_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    location_source = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    location_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_deal_notification_recipients", x => x.id);
                    table.ForeignKey(
                        name: "FK_deal_notification_recipients_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_flash_deals_publication_id",
                table: "flash_deals",
                column: "publication_id");

            migrationBuilder.CreateIndex(
                name: "ix_flash_deals_status_feed_published_at_id",
                table: "flash_deals",
                columns: new[] { "status", "feed_published_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_deal_notification_recipients_pub_status",
                table: "deal_notification_recipients",
                columns: new[] { "publication_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_deal_notification_recipients_user_id",
                table: "deal_notification_recipients",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ux_deal_notification_recipients_pub_user",
                table: "deal_notification_recipients",
                columns: new[] { "publication_id", "user_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "deal_notification_recipients");

            migrationBuilder.DropIndex(
                name: "ix_flash_deals_publication_id",
                table: "flash_deals");

            migrationBuilder.DropIndex(
                name: "ix_flash_deals_status_feed_published_at_id",
                table: "flash_deals");

            migrationBuilder.DropColumn(
                name: "address_version",
                table: "user_addresses");

            migrationBuilder.DropColumn(
                name: "feed_published_at",
                table: "flash_deals");

            migrationBuilder.DropColumn(
                name: "publication_id",
                table: "flash_deals");

            migrationBuilder.DropColumn(
                name: "visibility_version",
                table: "flash_deals");
        }
    }
}
