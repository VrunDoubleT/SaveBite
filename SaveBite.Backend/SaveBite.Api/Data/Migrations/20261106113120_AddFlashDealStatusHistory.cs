using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaveBite.Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFlashDealStatusHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "flash_deal_status_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    flash_deal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    to_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    changed_by_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    changed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    note = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_flash_deal_status_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_flash_deal_status_history_flash_deals_flash_deal_id",
                        column: x => x.flash_deal_id,
                        principalTable: "flash_deals",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_flash_deal_status_history_flash_deal_id_created_at",
                table: "flash_deal_status_history",
                columns: new[] { "flash_deal_id", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "flash_deal_status_history");
        }
    }
}
