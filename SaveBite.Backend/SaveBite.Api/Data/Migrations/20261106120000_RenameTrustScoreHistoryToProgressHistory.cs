using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaveBite.Backend.Data.Migrations
{
    /// <summary>
    /// trust_score_history  ->  trust_progress_history
    /// Adds value_before / value_delta / value_after (accumulated order value, same idea as the score columns)
    /// and back-fills them for the rows that already exist.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20261106120000_RenameTrustScoreHistoryToProgressHistory")]
    public partial class RenameTrustScoreHistoryToProgressHistory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Rename table (+ PK, indexes, foreign keys so the names stay consistent with EF conventions).
            migrationBuilder.RenameTable(name: "trust_score_history", newName: "trust_progress_history");

            migrationBuilder.Sql(@"ALTER TABLE trust_progress_history RENAME CONSTRAINT ""PK_trust_score_history"" TO ""PK_trust_progress_history"";");
            migrationBuilder.Sql(@"ALTER TABLE trust_progress_history RENAME CONSTRAINT ""FK_trust_score_history_no_show_penalties_no_show_penalty_id"" TO ""FK_trust_progress_history_no_show_penalties_no_show_penalty_id"";");
            migrationBuilder.Sql(@"ALTER TABLE trust_progress_history RENAME CONSTRAINT ""FK_trust_score_history_orders_order_id"" TO ""FK_trust_progress_history_orders_order_id"";");
            migrationBuilder.Sql(@"ALTER TABLE trust_progress_history RENAME CONSTRAINT ""FK_trust_score_history_trust_score_rules_rule_id"" TO ""FK_trust_progress_history_trust_score_rules_rule_id"";");
            migrationBuilder.Sql(@"ALTER TABLE trust_progress_history RENAME CONSTRAINT ""FK_trust_score_history_users_user_id"" TO ""FK_trust_progress_history_users_user_id"";");

            migrationBuilder.RenameIndex(name: "IX_trust_score_history_no_show_penalty_id", table: "trust_progress_history", newName: "IX_trust_progress_history_no_show_penalty_id");
            migrationBuilder.RenameIndex(name: "IX_trust_score_history_order_id", table: "trust_progress_history", newName: "IX_trust_progress_history_order_id");
            migrationBuilder.RenameIndex(name: "IX_trust_score_history_rule_id", table: "trust_progress_history", newName: "IX_trust_progress_history_rule_id");
            migrationBuilder.RenameIndex(name: "IX_trust_score_history_user_id", table: "trust_progress_history", newName: "IX_trust_progress_history_user_id");

            // 2) New money columns.
            migrationBuilder.AddColumn<decimal>(name: "value_before", table: "trust_progress_history",
                type: "numeric(12,2)", precision: 12, scale: 2, nullable: false, defaultValue: 0m);
            migrationBuilder.AddColumn<decimal>(name: "value_delta", table: "trust_progress_history",
                type: "numeric(12,2)", precision: 12, scale: 2, nullable: false, defaultValue: 0m);
            migrationBuilder.AddColumn<decimal>(name: "value_after", table: "trust_progress_history",
                type: "numeric(12,2)", precision: 12, scale: 2, nullable: false, defaultValue: 0m);

            // 3) Back-fill existing rows: completed orders carried the credited order value, the rest carried none.
            migrationBuilder.Sql(@"
UPDATE trust_progress_history h
SET value_delta = COALESCE(p.credited_order_value, 0)
FROM trust_order_processing p
WHERE h.event_type = 'OrderCompleted' AND h.order_id = p.order_id;");

            // Running total per customer (oldest -> newest) gives before/after for every row.
            migrationBuilder.Sql(@"
WITH running AS (
    SELECT id,
           SUM(value_delta) OVER (PARTITION BY user_id ORDER BY created_at, id
                                  ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) AS total
    FROM trust_progress_history
)
UPDATE trust_progress_history h
SET value_after = r.total,
    value_before = r.total - h.value_delta
FROM running r
WHERE r.id = h.id;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "value_before", table: "trust_progress_history");
            migrationBuilder.DropColumn(name: "value_delta", table: "trust_progress_history");
            migrationBuilder.DropColumn(name: "value_after", table: "trust_progress_history");

            migrationBuilder.RenameIndex(name: "IX_trust_progress_history_no_show_penalty_id", table: "trust_progress_history", newName: "IX_trust_score_history_no_show_penalty_id");
            migrationBuilder.RenameIndex(name: "IX_trust_progress_history_order_id", table: "trust_progress_history", newName: "IX_trust_score_history_order_id");
            migrationBuilder.RenameIndex(name: "IX_trust_progress_history_rule_id", table: "trust_progress_history", newName: "IX_trust_score_history_rule_id");
            migrationBuilder.RenameIndex(name: "IX_trust_progress_history_user_id", table: "trust_progress_history", newName: "IX_trust_score_history_user_id");

            migrationBuilder.Sql(@"ALTER TABLE trust_progress_history RENAME CONSTRAINT ""FK_trust_progress_history_users_user_id"" TO ""FK_trust_score_history_users_user_id"";");
            migrationBuilder.Sql(@"ALTER TABLE trust_progress_history RENAME CONSTRAINT ""FK_trust_progress_history_trust_score_rules_rule_id"" TO ""FK_trust_score_history_trust_score_rules_rule_id"";");
            migrationBuilder.Sql(@"ALTER TABLE trust_progress_history RENAME CONSTRAINT ""FK_trust_progress_history_orders_order_id"" TO ""FK_trust_score_history_orders_order_id"";");
            migrationBuilder.Sql(@"ALTER TABLE trust_progress_history RENAME CONSTRAINT ""FK_trust_progress_history_no_show_penalties_no_show_penalty_id"" TO ""FK_trust_score_history_no_show_penalties_no_show_penalty_id"";");
            migrationBuilder.Sql(@"ALTER TABLE trust_progress_history RENAME CONSTRAINT ""PK_trust_progress_history"" TO ""PK_trust_score_history"";");

            migrationBuilder.RenameTable(name: "trust_progress_history", newName: "trust_score_history");
        }
    }
}
