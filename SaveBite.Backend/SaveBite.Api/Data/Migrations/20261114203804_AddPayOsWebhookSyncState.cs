using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaveBite.Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPayOsWebhookSyncState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "payos_webhook_sync_states",
                columns: table => new
                {
                    AccountId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CredentialVersion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    WebhookUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    EndpointSession = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CredentialsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    NextAttemptAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastAttemptAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SucceededAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastErrorCode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payos_webhook_sync_states", x => x.AccountId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_payos_webhook_sync_states_Status_NextAttemptAt",
                table: "payos_webhook_sync_states",
                columns: new[] { "Status", "NextAttemptAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payos_webhook_sync_states");
        }
    }
}
