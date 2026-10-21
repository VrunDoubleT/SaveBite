using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SaveBite.Backend.Data;

namespace SaveBite.Backend.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261019090000_AllowRemainingPaymentRetries")]
public sealed class AllowRemainingPaymentRetries : Migration
{
    // Retain closed attempts while allowing only one pending remaining-payment request per order.
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "ux_payments_remaining_per_order", table: "payments");
        migrationBuilder.CreateIndex(name: "ux_payments_remaining_per_order", table: "payments", column: "order_id",
            unique: true, filter: "\"payment_type\" = 'Remaining' AND \"status\" = 'Pending'");
    }

    // PostgreSQL rejects rollback if retry history would violate the original one-row restriction.
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "ux_payments_remaining_per_order", table: "payments");
        migrationBuilder.CreateIndex(name: "ux_payments_remaining_per_order", table: "payments", column: "order_id",
            unique: true, filter: "\"payment_type\" = 'Remaining'");
    }
}
