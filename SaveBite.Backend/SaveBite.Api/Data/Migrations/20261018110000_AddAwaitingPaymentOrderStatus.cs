using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SaveBite.Backend.Data;

namespace SaveBite.Backend.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261018110000_AddAwaitingPaymentOrderStatus")]
public sealed class AddAwaitingPaymentOrderStatus : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Reclassify existing unpaid checkout orders while leaving paid and legacy orders unchanged.
        migrationBuilder.Sql("""
            WITH changed AS (
                UPDATE orders o SET status = 'AwaitingPayment'
                WHERE o.status = 'Pending' AND o.checkout_quote_id IS NOT NULL AND o.required_online_amount > 0
                  AND NOT EXISTS (
                    SELECT 1 FROM payments p WHERE p.order_id = o.id
                      AND p.payment_type IN ('Full', 'Deposit') AND p.status = 'Succeeded')
                RETURNING o.id
            )
            UPDATE order_status_history h SET to_status = 'AwaitingPayment'
            WHERE h.order_id IN (SELECT id FROM changed) AND h.to_status = 'Pending';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE orders SET status = 'Pending' WHERE status = 'AwaitingPayment';
            UPDATE order_status_history SET to_status = 'Pending' WHERE to_status = 'AwaitingPayment';
            UPDATE order_status_history SET from_status = 'Pending' WHERE from_status = 'AwaitingPayment';
            """);
    }
}
