using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaveBite.Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProductWarnings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "product_warnings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShopId = table.Column<Guid>(type: "uuid", nullable: false),
                    IssuedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    BaselineRevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    RequiredCorrection = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ProductName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DueAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FirstEditedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DealBlocked = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_warnings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_product_warnings_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_product_warnings_product_content_revisions_BaselineRevision~",
                        column: x => x.BaselineRevisionId,
                        principalTable: "product_content_revisions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_product_warnings_shops_ShopId",
                        column: x => x.ShopId,
                        principalTable: "shops",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_product_warnings_users_IssuedByUserId",
                        column: x => x.IssuedByUserId,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_product_warnings_BaselineRevisionId",
                table: "product_warnings",
                column: "BaselineRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_product_warnings_IssuedByUserId",
                table: "product_warnings",
                column: "IssuedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_product_warnings_ProductId",
                table: "product_warnings",
                column: "ProductId",
                unique: true,
                filter: "\"ResolvedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_product_warnings_ResolvedAt_DueAt",
                table: "product_warnings",
                columns: new[] { "ResolvedAt", "DueAt" });

            migrationBuilder.CreateIndex(
                name: "IX_product_warnings_ShopId",
                table: "product_warnings",
                column: "ShopId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_warnings");
        }
    }
}
