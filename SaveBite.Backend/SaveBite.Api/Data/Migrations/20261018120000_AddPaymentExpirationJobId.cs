using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SaveBite.Backend.Data;

namespace SaveBite.Backend.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261018120000_AddPaymentExpirationJobId")]
public sealed class AddPaymentExpirationJobId : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "payment_expiration_job_id",
            table: "orders",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "payment_expiration_job_id", table: "orders");
    }
}
