using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaveBite.Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTrustPolicyVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "applied_policy_version",
                table: "user_trust_scores",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "trust_policy_version",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    current_version = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trust_policy_version", x => x.id);
                });

            migrationBuilder.InsertData(
                table: "trust_policy_version",
                columns: new[] { "id", "current_version", "updated_at" },
                values: new object[] { 1, 1L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.CreateIndex(
                name: "ix_user_trust_scores_applied_policy_version",
                table: "user_trust_scores",
                column: "applied_policy_version");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "trust_policy_version");

            migrationBuilder.DropIndex(
                name: "ix_user_trust_scores_applied_policy_version",
                table: "user_trust_scores");

            migrationBuilder.DropColumn(
                name: "applied_policy_version",
                table: "user_trust_scores");
        }
    }
}
