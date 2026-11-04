using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaveBite.Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDeadlineRecoveryState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "suspended_for_platform_fees",
                table: "shops",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "end_job_id",
                table: "flash_deals",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "lifecycle_synced_at",
                table: "flash_deals",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "open_job_id",
                table: "flash_deals",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "suspended_for_platform_fees",
                table: "shops");

            migrationBuilder.DropColumn(
                name: "end_job_id",
                table: "flash_deals");

            migrationBuilder.DropColumn(
                name: "lifecycle_synced_at",
                table: "flash_deals");

            migrationBuilder.DropColumn(
                name: "open_job_id",
                table: "flash_deals");
        }
    }
}
