using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaveBite.Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AllowMultipleRefundFeeAdjustments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_platform_fee_adjustments_refund_id",
                table: "platform_fee_adjustments");

            migrationBuilder.CreateIndex(
                name: "IX_platform_fee_adjustments_refund_id",
                table: "platform_fee_adjustments",
                column: "refund_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_platform_fee_adjustments_refund_id",
                table: "platform_fee_adjustments");

            migrationBuilder.CreateIndex(
                name: "IX_platform_fee_adjustments_refund_id",
                table: "platform_fee_adjustments",
                column: "refund_id",
                unique: true);
        }
    }
}
