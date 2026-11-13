using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaveBite.Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPayOsVerificationAndFeePaymentPause : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_invalid",
                table: "platform_payos_configs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "revision",
                table: "platform_payos_configs",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DateTime>(
                name: "verified_at",
                table: "platform_payos_configs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "payment_pause_reason",
                table: "platform_fee_statements",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "payment_paused_at",
                table: "platform_fee_statements",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "payment_resume_revision",
                table: "platform_fee_statements",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "gateway_checkout_url",
                table: "platform_fee_payments",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "gateway_expires_at",
                table: "platform_fee_payments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "gateway_qr_code",
                table: "platform_fee_payments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "protected_payos_credentials",
                table: "platform_fee_payments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "protected_payos_credentials",
                table: "payments",
                type: "text",
                nullable: true);

            // Existing credentials have never been verified. Do not penalize shops during rollout.
            migrationBuilder.Sql("""
                INSERT INTO platform_fee_statement_histories
                    (id, statement_id, change_type, previous_amount, change_amount, new_amount, reason, created_at)
                SELECT gen_random_uuid(), id, 'PaymentPaused', total_fee_amount, 0, total_fee_amount,
                    'Payment paused pending platform payOS credential verification.', CURRENT_TIMESTAMP
                FROM platform_fee_statements WHERE total_fee_amount > 0 AND status IN ('Issued', 'Overdue');
                UPDATE platform_fee_statements SET payment_pause_reason = 'PayOsNotVerified',
                    payment_paused_at = CURRENT_TIMESTAMP, due_at = NULL, status = 'Issued', updated_at = CURRENT_TIMESTAMP
                WHERE total_fee_amount > 0 AND status IN ('Issued', 'Overdue');
                """);

            migrationBuilder.AddCheckConstraint(
                name: "ck_platform_fee_statement_paused_deadline",
                table: "platform_fee_statements",
                sql: "payment_pause_reason IS NULL OR due_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_platform_fee_statement_paused_deadline",
                table: "platform_fee_statements");

            migrationBuilder.DropColumn(
                name: "is_invalid",
                table: "platform_payos_configs");

            migrationBuilder.DropColumn(
                name: "revision",
                table: "platform_payos_configs");

            migrationBuilder.DropColumn(
                name: "verified_at",
                table: "platform_payos_configs");

            migrationBuilder.DropColumn(
                name: "payment_pause_reason",
                table: "platform_fee_statements");

            migrationBuilder.DropColumn(
                name: "payment_paused_at",
                table: "platform_fee_statements");

            migrationBuilder.DropColumn(
                name: "payment_resume_revision",
                table: "platform_fee_statements");

            migrationBuilder.DropColumn(
                name: "gateway_checkout_url",
                table: "platform_fee_payments");

            migrationBuilder.DropColumn(
                name: "gateway_expires_at",
                table: "platform_fee_payments");

            migrationBuilder.DropColumn(
                name: "gateway_qr_code",
                table: "platform_fee_payments");

            migrationBuilder.DropColumn(
                name: "protected_payos_credentials",
                table: "platform_fee_payments");

            migrationBuilder.DropColumn(
                name: "protected_payos_credentials",
                table: "payments");
        }
    }
}
