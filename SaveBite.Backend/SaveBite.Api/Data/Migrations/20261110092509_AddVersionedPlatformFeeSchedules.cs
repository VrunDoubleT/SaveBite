using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaveBite.Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVersionedPlatformFeeSchedules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "activation_processed_at",
                table: "platform_fee_configs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "closed_at",
                table: "platform_fee_configs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "closed_by",
                table: "platform_fee_configs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "closure_reason",
                table: "platform_fee_configs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "lifecycle_status",
                table: "platform_fee_configs",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Published");

            migrationBuilder.AddColumn<string>(
                name: "reason",
                table: "platform_fee_configs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "replaces_config_id",
                table: "platform_fee_configs",
                type: "uuid",
                nullable: true);

            // Existing effective versions predate activation notifications; do not replay old announcements.
            // Future versions stay unprocessed and will be announced normally when their start time is reached.
            migrationBuilder.Sql("""
                UPDATE platform_fee_configs SET activation_processed_at = CURRENT_TIMESTAMP
                WHERE effective_from <= CURRENT_TIMESTAMP;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_platform_fee_configs_closed_by",
                table: "platform_fee_configs",
                column: "closed_by");

            migrationBuilder.CreateIndex(
                name: "IX_platform_fee_configs_replaces_config_id",
                table: "platform_fee_configs",
                column: "replaces_config_id");

            migrationBuilder.CreateIndex(
                name: "ux_platform_fee_published_start",
                table: "platform_fee_configs",
                column: "effective_from",
                unique: true,
                filter: "lifecycle_status = 'Published'");

            migrationBuilder.AddCheckConstraint(
                name: "ck_platform_fee_lifecycle",
                table: "platform_fee_configs",
                sql: "lifecycle_status IN ('Published', 'Cancelled', 'Superseded')");

            migrationBuilder.AddForeignKey(
                name: "FK_platform_fee_configs_platform_fee_configs_replaces_config_id",
                table: "platform_fee_configs",
                column: "replaces_config_id",
                principalTable: "platform_fee_configs",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_platform_fee_configs_users_closed_by",
                table: "platform_fee_configs",
                column: "closed_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_platform_fee_configs_platform_fee_configs_replaces_config_id",
                table: "platform_fee_configs");

            migrationBuilder.DropForeignKey(
                name: "FK_platform_fee_configs_users_closed_by",
                table: "platform_fee_configs");

            migrationBuilder.DropIndex(
                name: "IX_platform_fee_configs_closed_by",
                table: "platform_fee_configs");

            migrationBuilder.DropIndex(
                name: "IX_platform_fee_configs_replaces_config_id",
                table: "platform_fee_configs");

            migrationBuilder.DropIndex(
                name: "ux_platform_fee_published_start",
                table: "platform_fee_configs");

            migrationBuilder.DropCheckConstraint(
                name: "ck_platform_fee_lifecycle",
                table: "platform_fee_configs");

            migrationBuilder.DropColumn(
                name: "activation_processed_at",
                table: "platform_fee_configs");

            migrationBuilder.DropColumn(
                name: "closed_at",
                table: "platform_fee_configs");

            migrationBuilder.DropColumn(
                name: "closed_by",
                table: "platform_fee_configs");

            migrationBuilder.DropColumn(
                name: "closure_reason",
                table: "platform_fee_configs");

            migrationBuilder.DropColumn(
                name: "lifecycle_status",
                table: "platform_fee_configs");

            migrationBuilder.DropColumn(
                name: "reason",
                table: "platform_fee_configs");

            migrationBuilder.DropColumn(
                name: "replaces_config_id",
                table: "platform_fee_configs");
        }
    }
}
