using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaveBite.Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderActionActors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "actor_name_snapshot",
                table: "staff_activity_logs",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "actor_role_snapshot",
                table: "staff_activity_logs",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "actor_name_snapshot",
                table: "order_status_history",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "actor_role_snapshot",
                table: "order_status_history",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "actor_name_snapshot",
                table: "staff_activity_logs");

            migrationBuilder.DropColumn(
                name: "actor_role_snapshot",
                table: "staff_activity_logs");

            migrationBuilder.DropColumn(
                name: "actor_name_snapshot",
                table: "order_status_history");

            migrationBuilder.DropColumn(
                name: "actor_role_snapshot",
                table: "order_status_history");
        }
    }
}
