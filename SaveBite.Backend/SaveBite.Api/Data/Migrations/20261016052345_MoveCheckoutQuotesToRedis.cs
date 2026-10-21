using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaveBite.Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class MoveCheckoutQuotesToRedis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_orders_checkout_quotes_checkout_quote_id",
                table: "orders");

            migrationBuilder.DropTable(
                name: "checkout_quotes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Dropped temporary quotes cannot be reconstructed for historical order foreign keys.
            throw new NotSupportedException("Quote storage migration is forward-only. Restore a database backup to revert it.");
        }
    }
}
