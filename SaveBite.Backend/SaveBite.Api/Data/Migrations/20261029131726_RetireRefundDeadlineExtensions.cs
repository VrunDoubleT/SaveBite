using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaveBite.Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class RetireRefundDeadlineExtensions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_refunds_active_order_scope",
                table: "refunds");

            // Close legacy overdue records while preserving their original audit history.
            migrationBuilder.Sql("""
                INSERT INTO refund_status_history
                    (id, refund_id, from_status, to_status, actor_role, actor_name, decision, note, created_at)
                SELECT gen_random_uuid(), id, status, 'Rejected', 'System', 'System', 'DeadlineExpired',
                    'Shop processing deadline expired.', CURRENT_TIMESTAMP
                FROM refunds WHERE status = 'Overdue';
                UPDATE refunds
                SET status = 'Rejected', reject_reason = 'Shop processing deadline expired.',
                    deadline_job_id = NULL, updated_at = CURRENT_TIMESTAMP
                WHERE status = 'Overdue';
                """);

            migrationBuilder.CreateIndex(
                name: "ux_refunds_active_order_scope",
                table: "refunds",
                column: "order_id",
                unique: true,
                filter: "\"status\" IN ('AwaitingCustomerDetails', 'PendingVerification', 'AwaitingTransfer')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_refunds_active_order_scope",
                table: "refunds");

            migrationBuilder.CreateIndex(
                name: "ux_refunds_active_order_scope",
                table: "refunds",
                column: "order_id",
                unique: true,
                filter: "\"status\" IN ('AwaitingCustomerDetails', 'PendingVerification', 'AwaitingTransfer', 'Overdue')");
        }
    }
}
