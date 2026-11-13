using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaveBite.Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMandatoryRefundSuspension : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "ux_refunds_active_order_scope", table: "refunds");
            migrationBuilder.CreateIndex(name: "ux_refunds_active_order_scope", table: "refunds", column: "order_id", unique: true,
                filter: "\"status\" IN ('AwaitingCustomerDetails', 'PendingVerification', 'AwaitingTransfer', 'Overdue')");
            migrationBuilder.AddColumn<bool>(
                name: "suspended_by_admin",
                table: "shops",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Existing manual suspensions must survive automatic debt settlement.
            migrationBuilder.Sql("UPDATE shops SET suspended_by_admin = TRUE WHERE status = 'Suspended' AND NOT suspended_for_platform_fees;");

            // Preserve the confirmation instant, extend only open shop-processing windows,
            // and let recovery schedule the new deadline. Old scheduled jobs are ignored.
            migrationBuilder.Sql("""
                UPDATE refunds
                SET review_deadline_at = destination_confirmed_at + INTERVAL '72 hours', deadline_job_id = NULL
                WHERE status IN ('PendingVerification', 'AwaitingTransfer') AND destination_confirmed_at IS NOT NULL;
                """);

            migrationBuilder.AddColumn<bool>(
                name: "suspended_for_overdue_refunds",
                table: "shops",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "ux_refunds_active_order_scope", table: "refunds");
            migrationBuilder.CreateIndex(name: "ux_refunds_active_order_scope", table: "refunds", column: "order_id", unique: true,
                filter: "\"status\" IN ('AwaitingCustomerDetails', 'PendingVerification', 'AwaitingTransfer')");
            migrationBuilder.DropColumn(
                name: "suspended_by_admin",
                table: "shops");

            migrationBuilder.DropColumn(
                name: "suspended_for_overdue_refunds",
                table: "shops");
        }
    }
}
