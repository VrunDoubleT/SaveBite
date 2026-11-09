using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaveBite.Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddShopStatusHistories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "shop_status_histories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    shop_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    to_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    was_suspended_by_admin = table.Column<bool>(type: "boolean", nullable: false),
                    was_suspended_for_platform_fees = table.Column<bool>(type: "boolean", nullable: false),
                    was_suspended_for_overdue_refunds = table.Column<bool>(type: "boolean", nullable: false),
                    suspended_by_admin = table.Column<bool>(type: "boolean", nullable: false),
                    suspended_for_platform_fees = table.Column<bool>(type: "boolean", nullable: false),
                    suspended_for_overdue_refunds = table.Column<bool>(type: "boolean", nullable: false),
                    changed_by_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    changed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shop_status_histories", x => x.id);
                    table.ForeignKey(
                        name: "FK_shop_status_histories_shops_shop_id",
                        column: x => x.shop_id,
                        principalTable: "shops",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shop_status_histories_users_changed_by_user_id",
                        column: x => x.changed_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_shop_status_histories_changed_by_user_id",
                table: "shop_status_histories",
                column: "changed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_shop_status_histories_shop_id_created_at",
                table: "shop_status_histories",
                columns: new[] { "shop_id", "created_at" });

            // Establish a clearly labelled snapshot, not an invented historical transition.
            // Never read or modify audit_logs to populate business history.
            migrationBuilder.Sql("""
                INSERT INTO shop_status_histories
                    (id, shop_id, from_status, to_status,
                     was_suspended_by_admin, was_suspended_for_platform_fees, was_suspended_for_overdue_refunds,
                     suspended_by_admin, suspended_for_platform_fees, suspended_for_overdue_refunds,
                     changed_by_type, changed_by_user_id, reason, created_at)
                SELECT gen_random_uuid(), id, NULL, status, FALSE, FALSE, FALSE,
                       suspended_by_admin, suspended_for_platform_fees, suspended_for_overdue_refunds,
                       'System', NULL,
                       'Initial status snapshot when dedicated shop history was introduced. '
                         || CASE WHEN suspended_by_admin THEN 'Suspended by an administrator. ' ELSE '' END
                         || CASE WHEN suspended_for_platform_fees THEN 'Overdue platform fees remain unpaid. ' ELSE '' END
                         || CASE WHEN suspended_for_overdue_refunds THEN 'Overdue mandatory refunds remain unpaid.' ELSE '' END,
                       NOW()
                FROM shops;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "shop_status_histories");
        }
    }
}
