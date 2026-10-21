using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SaveBite.Backend.Data;

#nullable disable
namespace SaveBite.Backend.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261016103000_EnsureShopOrderPaymentAccounting")]
public sealed class EnsureShopOrderPaymentAccounting : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_payments_order_id\" ON payments (order_id)");
        migrationBuilder.Sql("UPDATE payments SET collected_amount = amount WHERE status = 'Succeeded' AND collected_amount < amount");
        migrationBuilder.Sql("UPDATE refunds SET transferred_amount = requested_amount WHERE status IN ('Transferred', 'Completed') AND transferred_amount = 0");
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Preserve existing financial accounting and the general payment lookup index.
    }
}
