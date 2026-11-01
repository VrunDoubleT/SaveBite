using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaveBite.Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class LinkRefundEvidenceToItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "refund_item_id",
                table: "refund_evidence_images",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_refund_evidence_images_refund_item_id",
                table: "refund_evidence_images",
                column: "refund_item_id");

            migrationBuilder.AddForeignKey(
                name: "FK_refund_evidence_images_refund_items_refund_item_id",
                table: "refund_evidence_images",
                column: "refund_item_id",
                principalTable: "refund_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_refund_evidence_images_refund_items_refund_item_id",
                table: "refund_evidence_images");

            migrationBuilder.DropIndex(
                name: "IX_refund_evidence_images_refund_item_id",
                table: "refund_evidence_images");

            migrationBuilder.DropColumn(
                name: "refund_item_id",
                table: "refund_evidence_images");
        }
    }
}
