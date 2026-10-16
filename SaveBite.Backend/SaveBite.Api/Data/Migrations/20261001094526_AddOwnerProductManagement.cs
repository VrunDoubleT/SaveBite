using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaveBite.Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOwnerProductManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_shop_id",
                table: "Products");

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "Products",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "version",
                table: "Products",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "public_id",
                table: "product_images",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "sort_order",
                table: "product_images",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "sort_order",
                table: "product_attributes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "public_id",
                table: "product_attribute_values",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "sort_order",
                table: "product_attribute_values",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Products_shop_id_deleted_at_created_at",
                table: "Products",
                columns: new[] { "shop_id", "deleted_at", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_shop_id_deleted_at_created_at",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "version",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "public_id",
                table: "product_images");

            migrationBuilder.DropColumn(
                name: "sort_order",
                table: "product_images");

            migrationBuilder.DropColumn(
                name: "sort_order",
                table: "product_attributes");

            migrationBuilder.DropColumn(
                name: "public_id",
                table: "product_attribute_values");

            migrationBuilder.DropColumn(
                name: "sort_order",
                table: "product_attribute_values");

            migrationBuilder.CreateIndex(
                name: "IX_Products_shop_id",
                table: "Products",
                column: "shop_id");
        }
    }
}
