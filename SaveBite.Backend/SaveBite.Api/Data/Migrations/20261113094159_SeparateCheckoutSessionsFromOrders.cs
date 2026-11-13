using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaveBite.Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeparateCheckoutSessionsFromOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "order_id",
                table: "payments",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "checkout_session_id",
                table: "payments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "checkout_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shop_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quote_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    snapshot_json = table.Column<string>(type: "jsonb", nullable: false),
                    payment_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_checkout_sessions", x => x.id);
                    table.ForeignKey(
                        name: "FK_checkout_sessions_shops_shop_id",
                        column: x => x.shop_id,
                        principalTable: "shops",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_checkout_sessions_users_customer_id",
                        column: x => x.customer_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_payments_checkout_session_id",
                table: "payments",
                column: "checkout_session_id");

            migrationBuilder.CreateIndex(
                name: "IX_checkout_sessions_customer_id_quote_id",
                table: "checkout_sessions",
                columns: new[] { "customer_id", "quote_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_checkout_sessions_customer_id_request_id",
                table: "checkout_sessions",
                columns: new[] { "customer_id", "request_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_checkout_sessions_shop_id",
                table: "checkout_sessions",
                column: "shop_id");

            migrationBuilder.CreateIndex(
                name: "IX_checkout_sessions_status_payment_expires_at",
                table: "checkout_sessions",
                columns: new[] { "status", "payment_expires_at" });

            migrationBuilder.AddForeignKey(
                name: "FK_payments_checkout_sessions_checkout_session_id",
                table: "payments",
                column: "checkout_session_id",
                principalTable: "checkout_sessions",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM payments WHERE order_id IS NULL) THEN
                        RAISE EXCEPTION 'Cannot downgrade while checkout payments without orders exist. Preserve their history before downgrading.';
                    END IF;
                END $$;
                """);
            migrationBuilder.DropForeignKey(
                name: "FK_payments_checkout_sessions_checkout_session_id",
                table: "payments");

            migrationBuilder.DropTable(
                name: "checkout_sessions");

            migrationBuilder.DropIndex(
                name: "IX_payments_checkout_session_id",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "checkout_session_id",
                table: "payments");

            migrationBuilder.AlterColumn<Guid>(
                name: "order_id",
                table: "payments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
