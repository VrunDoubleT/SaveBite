using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaveBite.Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddActiveRefundScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }
    }
}
