using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaveBite.Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyShopPickupCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_orders_order_code",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "IX_orders_shop_id",
                table: "orders");

            migrationBuilder.AddColumn<DateOnly>(
                name: "order_date",
                table: "orders",
                type: "date",
                nullable: false,
                computedColumnSql: "(created_at AT TIME ZONE 'Asia/Ho_Chi_Minh')::date",
                stored: true);

            migrationBuilder.CreateIndex(
                name: "ux_orders_shop_date_code",
                table: "orders",
                columns: new[] { "shop_id", "order_date", "order_code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_orders_shop_date_code",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "order_date",
                table: "orders");

            migrationBuilder.CreateIndex(
                name: "IX_orders_order_code",
                table: "orders",
                column: "order_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_orders_shop_id",
                table: "orders",
                column: "shop_id");
        }
    }
}
