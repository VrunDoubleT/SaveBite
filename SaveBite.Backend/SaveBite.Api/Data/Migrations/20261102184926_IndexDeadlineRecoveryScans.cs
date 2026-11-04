using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaveBite.Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class IndexDeadlineRecoveryScans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_orders_status_hard_deadline_at",
                table: "orders",
                columns: new[] { "status", "hard_deadline_at" });

            migrationBuilder.CreateIndex(
                name: "IX_orders_status_payment_expires_at",
                table: "orders",
                columns: new[] { "status", "payment_expires_at" });

            migrationBuilder.CreateIndex(
                name: "ix_order_outbox_pending",
                table: "order_event_outbox",
                column: "id",
                filter: "\"published_at\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_flash_deals_status_order_end_time",
                table: "flash_deals",
                columns: new[] { "status", "order_end_time" });

            migrationBuilder.CreateIndex(
                name: "IX_flash_deals_status_sale_start_time",
                table: "flash_deals",
                columns: new[] { "status", "sale_start_time" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_orders_status_hard_deadline_at",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "IX_orders_status_payment_expires_at",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "ix_order_outbox_pending",
                table: "order_event_outbox");

            migrationBuilder.DropIndex(
                name: "IX_flash_deals_status_order_end_time",
                table: "flash_deals");

            migrationBuilder.DropIndex(
                name: "IX_flash_deals_status_sale_start_time",
                table: "flash_deals");
        }
    }
}
