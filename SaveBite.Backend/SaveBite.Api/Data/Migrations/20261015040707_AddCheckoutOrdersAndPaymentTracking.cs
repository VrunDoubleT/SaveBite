using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaveBite.Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCheckoutOrdersAndPaymentTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_orders_customer_id",
                table: "orders");

            migrationBuilder.AddColumn<string>(
                name: "gateway_checkout_url",
                table: "payments",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "gateway_creation_attempted",
                table: "payments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "gateway_order_code",
                table: "payments",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "checkout_policy_snapshot_json",
                table: "orders",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "checkout_quote_id",
                table: "orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "checkout_request_id",
                table: "orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "inventory_reservation_active",
                table: "orders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "payment_expires_at",
                table: "orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "payment_review_required",
                table: "orders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "required_online_amount",
                table: "orders",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "checkout_quotes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shop_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    location_json = table.Column<string>(type: "jsonb", nullable: false),
                    snapshot_json = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_checkout_quotes", x => x.id);
                    table.ForeignKey(
                        name: "FK_checkout_quotes_shops_shop_id",
                        column: x => x.shop_id,
                        principalTable: "shops",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_checkout_quotes_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_payments_gateway_order_code",
                table: "payments",
                column: "gateway_order_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_orders_checkout_quote_id",
                table: "orders",
                column: "checkout_quote_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_orders_customer_id_checkout_request_id",
                table: "orders",
                columns: new[] { "customer_id", "checkout_request_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_orders_inventory_reservation_active_payment_expires_at",
                table: "orders",
                columns: new[] { "inventory_reservation_active", "payment_expires_at" });

            migrationBuilder.CreateIndex(
                name: "IX_checkout_quotes_shop_id",
                table: "checkout_quotes",
                column: "shop_id");

            migrationBuilder.CreateIndex(
                name: "IX_checkout_quotes_user_id_expires_at",
                table: "checkout_quotes",
                columns: new[] { "user_id", "expires_at" });

            migrationBuilder.AddForeignKey(
                name: "FK_orders_checkout_quotes_checkout_quote_id",
                table: "orders",
                column: "checkout_quote_id",
                principalTable: "checkout_quotes",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_orders_checkout_quotes_checkout_quote_id",
                table: "orders");

            migrationBuilder.DropTable(
                name: "checkout_quotes");

            migrationBuilder.DropIndex(
                name: "IX_payments_gateway_order_code",
                table: "payments");

            migrationBuilder.DropIndex(
                name: "IX_orders_checkout_quote_id",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "IX_orders_customer_id_checkout_request_id",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "IX_orders_inventory_reservation_active_payment_expires_at",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "gateway_checkout_url",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "gateway_creation_attempted",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "gateway_order_code",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "checkout_policy_snapshot_json",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "checkout_quote_id",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "checkout_request_id",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "inventory_reservation_active",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "payment_expires_at",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "payment_review_required",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "required_online_amount",
                table: "orders");

            migrationBuilder.CreateIndex(
                name: "IX_orders_customer_id",
                table: "orders",
                column: "customer_id");
        }
    }
}
