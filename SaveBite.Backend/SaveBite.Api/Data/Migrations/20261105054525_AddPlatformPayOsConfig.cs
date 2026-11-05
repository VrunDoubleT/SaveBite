using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaveBite.Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformPayOsConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "platform_payos_config_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    changed_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    changed_fields_json = table.Column<string>(type: "jsonb", nullable: false),
                    previous_masked_client_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    masked_client_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    previous_frontend_base_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    frontend_base_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_platform_payos_config_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_platform_payos_config_history_users_changed_by_user_id",
                        column: x => x.changed_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "platform_payos_configs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    client_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    protected_api_key = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    protected_checksum_key = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    frontend_base_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_platform_payos_configs", x => x.id);
                    table.ForeignKey(
                        name: "FK_platform_payos_configs_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_platform_payos_config_history_changed_at",
                table: "platform_payos_config_history",
                column: "changed_at");

            migrationBuilder.CreateIndex(
                name: "IX_platform_payos_config_history_changed_by_user_id",
                table: "platform_payos_config_history",
                column: "changed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_platform_payos_configs_updated_by_user_id",
                table: "platform_payos_configs",
                column: "updated_by_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "platform_payos_config_history");

            migrationBuilder.DropTable(
                name: "platform_payos_configs");
        }
    }
}
