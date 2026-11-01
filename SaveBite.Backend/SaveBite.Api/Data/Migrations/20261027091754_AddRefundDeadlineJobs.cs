using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaveBite.Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRefundDeadlineJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_refunds_active_order_scope",
                table: "refunds");

            migrationBuilder.AddColumn<string>(
                name: "deadline_job_id",
                table: "refunds",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            // Extend only the former default policy; preserve administrative extensions.
            migrationBuilder.Sql("""
                UPDATE refunds SET review_deadline_at = pending_verification_at + INTERVAL '24 hours'
                WHERE refund_type = 'CustomerRequest' AND status IN ('PendingVerification', 'AwaitingTransfer')
                  AND pending_verification_at IS NOT NULL
                  AND review_deadline_at = pending_verification_at + INTERVAL '12 hours';
                """);

            migrationBuilder.CreateIndex(
                name: "ux_refunds_active_order_scope",
                table: "refunds",
                column: "order_id",
                unique: true,
                filter: "\"status\" IN ('AwaitingCustomerDetails', 'PendingVerification', 'AwaitingTransfer', 'Overdue')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_refunds_active_order_scope",
                table: "refunds");

            migrationBuilder.DropColumn(
                name: "deadline_job_id",
                table: "refunds");

            migrationBuilder.CreateIndex(
                name: "ux_refunds_active_order_scope",
                table: "refunds",
                column: "order_id",
                unique: true,
                filter: "\"status\" IN ('AwaitingCustomerDetails', 'PendingVerification', 'AwaitingTransfer')");
        }
    }
}
