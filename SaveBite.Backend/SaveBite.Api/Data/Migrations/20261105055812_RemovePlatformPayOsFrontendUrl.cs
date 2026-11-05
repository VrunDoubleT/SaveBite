using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaveBite.Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemovePlatformPayOsFrontendUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "frontend_base_url",
                table: "platform_payos_configs");

            migrationBuilder.DropColumn(
                name: "frontend_base_url",
                table: "platform_payos_config_history");

            migrationBuilder.DropColumn(
                name: "previous_frontend_base_url",
                table: "platform_payos_config_history");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "frontend_base_url",
                table: "platform_payos_configs",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "frontend_base_url",
                table: "platform_payos_config_history",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "previous_frontend_base_url",
                table: "platform_payos_config_history",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);
        }
    }
}
