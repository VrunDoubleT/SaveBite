using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaveBite.Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameSharedEventOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "order_event_outbox",
                newName: "event_outbox");

            migrationBuilder.RenameIndex(
                name: "ix_order_outbox_pending",
                table: "event_outbox",
                newName: "ix_event_outbox_pending");

            migrationBuilder.RenameIndex(
                name: "IX_order_event_outbox_event_key",
                table: "event_outbox",
                newName: "IX_event_outbox_event_key");

            migrationBuilder.Sql("ALTER TABLE event_outbox RENAME CONSTRAINT \"PK_order_event_outbox\" TO \"PK_event_outbox\"");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "event_outbox",
                newName: "order_event_outbox");

            migrationBuilder.RenameIndex(
                name: "ix_event_outbox_pending",
                table: "order_event_outbox",
                newName: "ix_order_outbox_pending");

            migrationBuilder.RenameIndex(
                name: "IX_event_outbox_event_key",
                table: "order_event_outbox",
                newName: "IX_order_event_outbox_event_key");

            migrationBuilder.Sql("ALTER TABLE order_event_outbox RENAME CONSTRAINT \"PK_event_outbox\" TO \"PK_order_event_outbox\"");
        }
    }
}
