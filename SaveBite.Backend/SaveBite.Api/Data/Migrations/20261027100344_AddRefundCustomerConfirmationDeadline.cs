using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaveBite.Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRefundCustomerConfirmationDeadline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_refunds_shop_cancellation_not_rejected",
                table: "refunds");

            migrationBuilder.AddColumn<DateTime>(
                name: "customer_details_deadline_at",
                table: "refunds",
                type: "timestamp with time zone",
                nullable: true);

            // Existing refunds follow the same absolute deadlines as newly created requests.
            migrationBuilder.Sql("""
                UPDATE refunds SET customer_details_deadline_at = created_at + INTERVAL '24 hours'
                WHERE refund_type = 'ShopCancellation' AND status = 'AwaitingCustomerDetails';
                UPDATE refunds SET review_deadline_at = destination_confirmed_at + INTERVAL '24 hours'
                WHERE status IN ('PendingVerification', 'AwaitingTransfer') AND review_deadline_at IS NULL
                  AND destination_confirmed_at IS NOT NULL;
                INSERT INTO refund_status_history (id, refund_id, from_status, to_status, actor_role, actor_name, decision, note, created_at)
                SELECT gen_random_uuid(), id, status, 'Rejected', 'System', 'System', 'DeadlineExpired',
                    'Shop processing deadline expired.', CURRENT_TIMESTAMP FROM refunds WHERE status = 'Overdue';
                UPDATE refunds SET status = 'Rejected', reject_reason = 'Shop processing deadline expired.',
                    deadline_job_id = NULL, updated_at = CURRENT_TIMESTAMP WHERE status = 'Overdue';
                """);

            migrationBuilder.AddCheckConstraint(
                name: "ck_refunds_shop_cancellation_not_rejected",
                table: "refunds",
                sql: "\"refund_type\" <> 'ShopCancellation' OR (\"status\" <> 'Rejected' OR \"reject_reason\" IN ('Customer confirmation deadline expired.', 'Shop processing deadline expired.'))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_refunds_shop_cancellation_not_rejected",
                table: "refunds");

            migrationBuilder.DropColumn(
                name: "customer_details_deadline_at",
                table: "refunds");

            migrationBuilder.AddCheckConstraint(
                name: "ck_refunds_shop_cancellation_not_rejected",
                table: "refunds",
                sql: "\"refund_type\" <> 'ShopCancellation' OR \"status\" <> 'Rejected'");
        }
    }
}
