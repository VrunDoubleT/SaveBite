using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaveBite.Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class InvalidateFeeQrWhenStatementAmountChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "collected_amount",
                table: "platform_fee_payments",
                type: "numeric(14,2)",
                precision: 14,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "gateway_closed_at",
                table: "platform_fee_payments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "gateway_invalidated_at",
                table: "platform_fee_payments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE platform_fee_payments
                SET collected_amount = amount, gateway_closed_at = COALESCE(paid_at, updated_at)
                WHERE status = 'Succeeded';
                UPDATE platform_fee_payments p
                SET gateway_invalidated_at = CURRENT_TIMESTAMP,
                    gateway_checkout_url = NULL, gateway_qr_code = NULL
                FROM platform_fee_statements s
                WHERE p.statement_id = s.id AND p.status = 'Pending'
                    AND p.amount <> GREATEST(0, s.total_fee_amount - COALESCE(
                        (SELECT SUM(receipt.amount) FROM platform_fee_payments receipt
                         WHERE receipt.statement_id = s.id AND receipt.status = 'Succeeded'), 0));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "collected_amount",
                table: "platform_fee_payments");

            migrationBuilder.DropColumn(
                name: "gateway_closed_at",
                table: "platform_fee_payments");

            migrationBuilder.DropColumn(
                name: "gateway_invalidated_at",
                table: "platform_fee_payments");
        }
    }
}
