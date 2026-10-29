using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaveBite.Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class BoundCustomerRefundConfirmation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Bound old customer requests from their original creation time, never from deployment time.
            migrationBuilder.Sql("""
                UPDATE refunds
                SET customer_details_deadline_at = created_at + INTERVAL '24 hours'
                WHERE refund_type = 'CustomerRequest' AND status = 'AwaitingCustomerDetails'
                  AND customer_details_deadline_at IS NULL;
                INSERT INTO refund_status_history
                    (id, refund_id, from_status, to_status, actor_role, actor_name, decision, note, created_at)
                SELECT gen_random_uuid(), id, status, 'Rejected', 'System', 'System', 'DeadlineExpired',
                    'Customer confirmation deadline expired.', CURRENT_TIMESTAMP
                FROM refunds WHERE refund_type = 'CustomerRequest' AND status = 'AwaitingCustomerDetails'
                  AND customer_details_deadline_at <= CURRENT_TIMESTAMP;
                UPDATE refunds
                SET status = 'Rejected', reject_reason = 'Customer confirmation deadline expired.',
                    deadline_job_id = NULL, updated_at = CURRENT_TIMESTAMP
                WHERE refund_type = 'CustomerRequest' AND status = 'AwaitingCustomerDetails'
                  AND customer_details_deadline_at <= CURRENT_TIMESTAMP;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // A rollback must not reopen already-expired financial requests.
        }
    }
}
