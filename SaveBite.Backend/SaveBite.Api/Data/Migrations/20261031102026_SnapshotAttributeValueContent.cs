using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaveBite.Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class SnapshotAttributeValueContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "attribute_values_json",
                table: "product_content_revisions",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "attribute_values_json",
                table: "product_content_revisions");
        }
    }
}
