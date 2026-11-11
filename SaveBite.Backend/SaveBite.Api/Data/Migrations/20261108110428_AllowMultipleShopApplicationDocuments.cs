using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaveBite.Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AllowMultipleShopApplicationDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_shop_application_current_document",
                table: "shop_application_documents");

            migrationBuilder.CreateIndex(
                name: "ix_shop_application_current_document",
                table: "shop_application_documents",
                columns: new[] { "application_id", "document_type" },
                filter: "\"is_current\" = TRUE AND \"document_type\" <> 'Other'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_shop_application_current_document",
                table: "shop_application_documents");

            migrationBuilder.CreateIndex(
                name: "ux_shop_application_current_document",
                table: "shop_application_documents",
                columns: new[] { "application_id", "document_type" },
                unique: true,
                filter: "\"is_current\" = TRUE AND \"document_type\" <> 'Other'");
        }
    }
}
