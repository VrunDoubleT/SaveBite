using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SaveBite.Backend.Data;

#nullable disable

namespace SaveBite.Backend.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261105100000_RemoveShopBankAccountFields")]
public partial class RemoveShopBankAccountFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "bank_name", table: "shop_applications");
        migrationBuilder.DropColumn(name: "bank_account_number", table: "shop_applications");
        migrationBuilder.DropColumn(name: "bank_account_holder", table: "shop_applications");

        migrationBuilder.DropColumn(name: "bank_name", table: "shop_payment_configs");
        migrationBuilder.DropColumn(name: "bank_account_number", table: "shop_payment_configs");
        migrationBuilder.DropColumn(name: "bank_account_holder", table: "shop_payment_configs");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "bank_name", table: "shop_applications",
            type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>(
            name: "bank_account_number", table: "shop_applications",
            type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>(
            name: "bank_account_holder", table: "shop_applications",
            type: "character varying(150)", maxLength: 150, nullable: false, defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "bank_name", table: "shop_payment_configs",
            type: "character varying(100)", maxLength: 100, nullable: true);
        migrationBuilder.AddColumn<string>(
            name: "bank_account_number", table: "shop_payment_configs",
            type: "character varying(50)", maxLength: 50, nullable: true);
        migrationBuilder.AddColumn<string>(
            name: "bank_account_holder", table: "shop_payment_configs",
            type: "character varying(150)", maxLength: 150, nullable: true);
    }
}
