using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaveBite.Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddShopOrderOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "destination_confirmed_at",
                table: "refunds",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "verification_attempts",
                table: "refunds",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "verification_code_hash",
                table: "refunds",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "verification_expires_at",
                table: "refunds",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "gateway_qr_code",
                table: "payments",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ux_payments_remaining_per_order",
                table: "payments",
                column: "order_id",
                unique: true,
                filter: "\"payment_type\" = 'Remaining'");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_payments_remaining_per_order",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "destination_confirmed_at",
                table: "refunds");

            migrationBuilder.DropColumn(
                name: "verification_attempts",
                table: "refunds");

            migrationBuilder.DropColumn(
                name: "verification_code_hash",
                table: "refunds");

            migrationBuilder.DropColumn(
                name: "verification_expires_at",
                table: "refunds");

            migrationBuilder.DropColumn(
                name: "gateway_qr_code",
                table: "payments");

        }
    }
}
