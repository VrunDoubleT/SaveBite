using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaveBite.Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformFeeStatementHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlatformFeeStatementHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StatementId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangeType = table.Column<int>(type: "integer", nullable: false),
                    PreviousAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    ChangeAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    NewAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    RelatedRefundId = table.Column<Guid>(type: "uuid", nullable: true),
                    RelatedOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformFeeStatementHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlatformFeeStatementHistories_platform_fee_statements_State~",
                        column: x => x.StatementId,
                        principalTable: "platform_fee_statements",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlatformFeeStatementHistories_StatementId",
                table: "PlatformFeeStatementHistories",
                column: "StatementId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlatformFeeStatementHistories");
        }
    }
}
