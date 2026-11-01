using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaveBite.Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddItemQuantityRefunds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "refunded_amount",
                table: "orders",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "order_refund_requests",
                columns: table => new
                {
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    refund_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_refund_requests", x => x.order_id);
                    table.ForeignKey(
                        name: "FK_order_refund_requests_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_order_refund_requests_refunds_refund_id",
                        column: x => x.refund_id,
                        principalTable: "refunds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "refund_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    refund_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    variant_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    image_url = table.Column<string>(type: "text", nullable: true),
                    purchased_quantity = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refund_items", x => x.id);
                    table.CheckConstraint("ck_refund_items_values", "quantity > 0 AND quantity <= purchased_quantity AND unit_price >= 0 AND amount >= 0");
                    table.ForeignKey(
                        name: "FK_refund_items_order_items_order_item_id",
                        column: x => x.order_item_id,
                        principalTable: "order_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_refund_items_refunds_refund_id",
                        column: x => x.refund_id,
                        principalTable: "refunds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_order_refund_requests_refund_id",
                table: "order_refund_requests",
                column: "refund_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refund_items_order_item_id",
                table: "refund_items",
                column: "order_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_refund_items_refund_id_order_item_id",
                table: "refund_items",
                columns: new[] { "refund_id", "order_item_id" },
                unique: true);
            // Preserve the lifetime submission limit for existing requests without deleting historical duplicates.
            migrationBuilder.Sql("""
                INSERT INTO order_refund_requests (order_id, refund_id, created_at)
                SELECT DISTINCT ON (order_id) order_id, id, created_at
                FROM refunds WHERE refund_type = 'CustomerRequest'
                ORDER BY order_id, created_at, id;

                UPDATE orders o SET refunded_amount = LEAST(o.total_amount, r.amount)
                FROM (SELECT order_id, SUM(transferred_amount) AS amount FROM refunds
                      WHERE status IN ('Transferred', 'Completed') GROUP BY order_id) r
                WHERE o.id = r.order_id;

                INSERT INTO refund_items (id, refund_id, order_item_id, product_name, variant_name,
                    purchased_quantity, quantity, unit_price, amount)
                SELECT gen_random_uuid(), r.id, i.id, i.product_name_snapshot, i.variant_name_snapshot,
                    i.quantity, i.quantity, i.unit_price, i.unit_price * i.quantity
                FROM refunds r JOIN orders o ON o.id = r.order_id JOIN order_items i ON i.order_id = o.id
                WHERE r.requested_amount = o.total_amount AND i.quantity > 0 AND i.unit_price >= 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "order_refund_requests");

            migrationBuilder.DropTable(
                name: "refund_items");

            migrationBuilder.DropColumn(
                name: "refunded_amount",
                table: "orders");
        }
    }
}
