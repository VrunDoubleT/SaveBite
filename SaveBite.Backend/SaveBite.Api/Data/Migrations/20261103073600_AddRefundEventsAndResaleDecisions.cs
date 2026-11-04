using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaveBite.Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRefundEventsAndResaleDecisions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_order_event_outbox_order_id_routing_key",
                table: "order_event_outbox");

            migrationBuilder.AddColumn<DateTime>(
                name: "fee_adjusted_at",
                table: "refunds",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "resale_decision_deadline_at",
                table: "orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "resale_decision_job_id",
                table: "orders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "resale_decided_at",
                table: "order_items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "resale_decided_by",
                table: "order_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "resale_decision_status",
                table: "order_items",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "NotRequired");

            migrationBuilder.AddColumn<int>(
                name: "resale_quantity",
                table: "order_items",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "unsellable_quantity",
                table: "order_items",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<Guid>(
                name: "order_id",
                table: "order_event_outbox",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "event_key",
                table: "order_event_outbox",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("UPDATE order_event_outbox SET event_key = 'legacy:' || id::text WHERE event_key = ''");

            migrationBuilder.CreateIndex(
                name: "IX_orders_resale_decision_deadline_at",
                table: "orders",
                column: "resale_decision_deadline_at");

            migrationBuilder.AddCheckConstraint(
                name: "ck_order_item_resale_quantities",
                table: "order_items",
                sql: "resale_quantity >= 0 AND unsellable_quantity >= 0 AND resale_quantity + unsellable_quantity <= quantity");

            migrationBuilder.CreateIndex(
                name: "IX_order_event_outbox_event_key",
                table: "order_event_outbox",
                column: "event_key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_orders_resale_decision_deadline_at",
                table: "orders");

            migrationBuilder.DropCheckConstraint(
                name: "ck_order_item_resale_quantities",
                table: "order_items");

            migrationBuilder.DropIndex(
                name: "IX_order_event_outbox_event_key",
                table: "order_event_outbox");

            migrationBuilder.DropColumn(
                name: "fee_adjusted_at",
                table: "refunds");

            migrationBuilder.DropColumn(
                name: "resale_decision_deadline_at",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "resale_decision_job_id",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "resale_decided_at",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "resale_decided_by",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "resale_decision_status",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "resale_quantity",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "unsellable_quantity",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "event_key",
                table: "order_event_outbox");

            migrationBuilder.AlterColumn<Guid>(
                name: "order_id",
                table: "order_event_outbox",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_order_event_outbox_order_id_routing_key",
                table: "order_event_outbox",
                columns: new[] { "order_id", "routing_key" },
                unique: true);
        }
    }
}
