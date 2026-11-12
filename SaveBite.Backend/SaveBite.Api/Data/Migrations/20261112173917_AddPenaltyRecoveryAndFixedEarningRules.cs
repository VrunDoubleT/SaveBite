using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaveBite.Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPenaltyRecoveryAndFixedEarningRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "created_by",
                table: "trust_score_rules",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "trust_score_rules",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "end_job_id",
                table: "recovery_requirement_rules",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "end_processed_at",
                table: "recovery_requirement_rules",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "start_job_id",
                table: "recovery_requirement_rules",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "start_processed_at",
                table: "recovery_requirement_rules",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "superseded_at",
                table: "no_show_penalties",
                type: "timestamp with time zone",
                nullable: true);

            // Retain historical versions and ledger references, but expose one fixed configuration per earning type.
            migrationBuilder.Sql("""
                WITH versions AS (
                    SELECT id, row_number() OVER (PARTITION BY rule_type ORDER BY
                        (effective_from <= CURRENT_TIMESTAMP AND (effective_to IS NULL OR effective_to > CURRENT_TIMESTAMP)) DESC,
                        (effective_from <= CURRENT_TIMESTAMP) DESC, effective_from DESC, id DESC) AS position
                    FROM trust_score_rules WHERE rule_type IN ('OnlineOrderCompleted', 'PayAtShopOrderCompleted')
                )
                UPDATE trust_score_rules r SET
                    effective_to = CASE WHEN v.position = 1 THEN NULL
                        ELSE COALESCE(r.effective_to, GREATEST(CURRENT_TIMESTAMP, r.effective_from + INTERVAL '1 microsecond')) END,
                    effective_from = CASE WHEN v.position = 1 THEN LEAST(r.effective_from, CURRENT_TIMESTAMP) ELSE r.effective_from END,
                    is_active = NOT (v.position > 1 AND r.effective_from > CURRENT_TIMESTAMP)
                FROM versions v WHERE r.id = v.id;

                WITH timeline AS (
                    SELECT id, lead(created_at) OVER (PARTITION BY user_id ORDER BY created_at, occurrence_number, id) AS next_at
                    FROM no_show_penalties
                )
                UPDATE no_show_penalties p SET status = 'Superseded', superseded_at = t.next_at
                FROM timeline t WHERE p.id = t.id AND p.status = 'Recovering' AND t.next_at IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE no_show_penalties SET status = 'Recovering' WHERE status = 'Superseded';
                UPDATE trust_score_rules SET created_by = (SELECT id FROM users ORDER BY created_at LIMIT 1) WHERE created_by IS NULL;
                DELETE FROM trust_score_rules WHERE created_by IS NULL;
                """);
            migrationBuilder.DropColumn(
                name: "is_active",
                table: "trust_score_rules");

            migrationBuilder.DropColumn(
                name: "end_job_id",
                table: "recovery_requirement_rules");

            migrationBuilder.DropColumn(
                name: "end_processed_at",
                table: "recovery_requirement_rules");

            migrationBuilder.DropColumn(
                name: "start_job_id",
                table: "recovery_requirement_rules");

            migrationBuilder.DropColumn(
                name: "start_processed_at",
                table: "recovery_requirement_rules");

            migrationBuilder.DropColumn(
                name: "superseded_at",
                table: "no_show_penalties");

            migrationBuilder.AlterColumn<Guid>(
                name: "created_by",
                table: "trust_score_rules",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
