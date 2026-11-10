using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaveBite.Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseAndStatementSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "shop_address_snapshot",
                table: "platform_fee_statements",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "shop_logo_url_snapshot",
                table: "platform_fee_statements",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "shop_name_snapshot",
                table: "platform_fee_statements",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "snapshot_captured_at",
                table: "platform_fee_statements",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "shop_address_snapshot",
                table: "orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "shop_logo_url_snapshot",
                table: "orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "shop_name_snapshot",
                table: "orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "snapshot_captured_at",
                table: "orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "product_image_url_snapshot",
                table: "order_items",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "snapshot_captured_at",
                table: "order_items",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "shop_address_snapshot",
                table: "platform_fee_statements");

            migrationBuilder.DropColumn(
                name: "shop_logo_url_snapshot",
                table: "platform_fee_statements");

            migrationBuilder.DropColumn(
                name: "shop_name_snapshot",
                table: "platform_fee_statements");

            migrationBuilder.DropColumn(
                name: "snapshot_captured_at",
                table: "platform_fee_statements");

            migrationBuilder.DropColumn(
                name: "shop_address_snapshot",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "shop_logo_url_snapshot",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "shop_name_snapshot",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "snapshot_captured_at",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "product_image_url_snapshot",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "snapshot_captured_at",
                table: "order_items");
        }
    }
}
