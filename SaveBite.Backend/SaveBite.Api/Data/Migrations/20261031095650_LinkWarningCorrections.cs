using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaveBite.Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class LinkWarningCorrections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "warning_id",
                table: "product_content_revisions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_content_revisions_warning_id",
                table: "product_content_revisions",
                column: "warning_id");

            migrationBuilder.AddForeignKey(
                name: "FK_product_content_revisions_product_warnings_warning_id",
                table: "product_content_revisions",
                column: "warning_id",
                principalTable: "product_warnings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_product_content_revisions_product_warnings_warning_id",
                table: "product_content_revisions");

            migrationBuilder.DropIndex(
                name: "IX_product_content_revisions_warning_id",
                table: "product_content_revisions");

            migrationBuilder.DropColumn(
                name: "warning_id",
                table: "product_content_revisions");
        }
    }
}
