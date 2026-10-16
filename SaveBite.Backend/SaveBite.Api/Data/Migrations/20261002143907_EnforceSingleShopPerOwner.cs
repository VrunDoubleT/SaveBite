using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaveBite.Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class EnforceSingleShopPerOwner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_shops_owner_user_id",
                table: "shops");

            migrationBuilder.CreateIndex(
                name: "IX_shops_owner_user_id",
                table: "shops",
                column: "owner_user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_shops_owner_user_id",
                table: "shops");

            migrationBuilder.CreateIndex(
                name: "IX_shops_owner_user_id",
                table: "shops",
                column: "owner_user_id");
        }
    }
}
