using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaveBite.Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformFeePayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "platform_fee_payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    statement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shop_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    payment_method = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    gateway_transaction_id = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    paid_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_platform_fee_payments", x => x.id);
                    table.CheckConstraint("ck_platform_fee_payment_amount", "\"amount\" > 0");
                    table.ForeignKey(
                        name: "FK_platform_fee_payments_platform_fee_statements_statement_id",
                        column: x => x.statement_id,
                        principalTable: "platform_fee_statements",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_platform_fee_payments_shops_shop_id",
                        column: x => x.shop_id,
                        principalTable: "shops",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_platform_fee_payments_gateway_transaction_id",
                table: "platform_fee_payments",
                column: "gateway_transaction_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_platform_fee_payments_shop_id",
                table: "platform_fee_payments",
                column: "shop_id");

            migrationBuilder.CreateIndex(
                name: "IX_platform_fee_payments_statement_id",
                table: "platform_fee_payments",
                column: "statement_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "platform_fee_payments");
        }
    }
}
