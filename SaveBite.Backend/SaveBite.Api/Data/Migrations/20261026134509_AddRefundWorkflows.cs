using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaveBite.Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRefundWorkflows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "credited_order_value",
                table: "trust_order_processing",
                type: "numeric(14,2)",
                precision: 14,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "accumulated_value_reclaimed_at",
                table: "refunds",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "pending_verification_at",
                table: "refunds",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "receiver_bank_bin",
                table: "refunds",
                type: "character varying(6)",
                maxLength: 6,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "review_deadline_at",
                table: "refunds",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "verification_issued_at",
                table: "refunds",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_platform_fee_exempt",
                table: "orders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "refund_status_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    refund_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    to_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    actor_role = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    actor_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    decision = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    note = table.Column<string>(type: "text", nullable: true),
                    transfer_reference = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    proof_url = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refund_status_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_refund_status_history_refunds_refund_id",
                        column: x => x.refund_id,
                        principalTable: "refunds",
                        principalColumn: "id");
                });

            // Preserve pre-existing credited values and import existing refund decisions without inventing history.
            migrationBuilder.Sql("""
                UPDATE trust_order_processing p SET credited_order_value = o.total_amount
                FROM orders o WHERE p.order_id = o.id AND o.status = 'Completed';

                UPDATE refunds SET verification_issued_at = verification_expires_at - INTERVAL '15 minutes'
                WHERE verification_expires_at IS NOT NULL;
                UPDATE refunds SET status = 'AwaitingCustomerDetails'
                WHERE status = 'PendingVerification' AND destination_confirmed_at IS NULL;
                UPDATE refunds SET pending_verification_at = COALESCE(destination_confirmed_at, reviewed_at, updated_at),
                    review_deadline_at = COALESCE(destination_confirmed_at, reviewed_at, updated_at) + INTERVAL '12 hours'
                WHERE refund_type = 'CustomerRequest' AND status IN ('PendingVerification', 'AwaitingTransfer');
                UPDATE orders o SET is_platform_fee_exempt = TRUE
                WHERE EXISTS (SELECT 1 FROM refunds f WHERE f.order_id = o.id
                    AND (f.refund_type = 'ShopCancellation' OR f.status IN ('AwaitingTransfer', 'Transferred', 'Completed')));

                INSERT INTO refund_status_history
                    (id, refund_id, to_status, actor_id, actor_role, actor_name, decision, note, transfer_reference, created_at)
                SELECT gen_random_uuid(), f.id, f.status, COALESCE(f.reviewed_by, f.initiated_by),
                    'Legacy', u.full_name, 'LegacySnapshot', 'Imported current state; earlier transitions were not recorded.',
                    f.transfer_reference, f.updated_at
                FROM refunds f JOIN users u ON u.id = COALESCE(f.reviewed_by, f.initiated_by);
                """);

            migrationBuilder.CreateIndex(
                name: "IX_refund_status_history_refund_id_created_at",
                table: "refund_status_history",
                columns: new[] { "refund_id", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "refund_status_history");

            migrationBuilder.DropColumn(
                name: "credited_order_value",
                table: "trust_order_processing");

            migrationBuilder.DropColumn(
                name: "accumulated_value_reclaimed_at",
                table: "refunds");

            migrationBuilder.DropColumn(
                name: "pending_verification_at",
                table: "refunds");

            migrationBuilder.DropColumn(
                name: "receiver_bank_bin",
                table: "refunds");

            migrationBuilder.DropColumn(
                name: "review_deadline_at",
                table: "refunds");

            migrationBuilder.DropColumn(
                name: "verification_issued_at",
                table: "refunds");

            migrationBuilder.DropColumn(
                name: "is_platform_fee_exempt",
                table: "orders");
        }
    }
}
