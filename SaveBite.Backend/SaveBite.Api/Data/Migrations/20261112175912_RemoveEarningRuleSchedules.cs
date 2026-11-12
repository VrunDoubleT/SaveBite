using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaveBite.Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveEarningRuleSchedules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Keep recorded score deltas intact while consolidating old rule references.
            migrationBuilder.Sql("""
                CREATE TEMP TABLE earning_rule_merge ON COMMIT DROP AS
                SELECT id, first_value(id) OVER (PARTITION BY rule_type ORDER BY
                    (effective_to IS NULL) DESC, effective_from DESC, created_at DESC, id DESC) AS keep_id
                FROM trust_score_rules
                WHERE rule_type IN ('OnlineOrderCompleted', 'PayAtShopOrderCompleted');

                UPDATE trust_progress_history h SET rule_id = m.keep_id
                FROM earning_rule_merge m WHERE h.rule_id = m.id AND m.id <> m.keep_id;
                DELETE FROM trust_score_rules r USING earning_rule_merge m
                WHERE r.id = m.id AND m.id <> m.keep_id;

                UPDATE trust_progress_history SET rule_id = NULL
                WHERE rule_id IN (SELECT id FROM trust_score_rules WHERE rule_type = 'NoShow');
                DELETE FROM trust_score_rules WHERE rule_type = 'NoShow';
                """);
            migrationBuilder.DropCheckConstraint(
                name: "ck_trust_score_rule_period",
                table: "trust_score_rules");

            migrationBuilder.DropColumn(
                name: "effective_from",
                table: "trust_score_rules");

            migrationBuilder.DropColumn(
                name: "effective_to",
                table: "trust_score_rules");

            migrationBuilder.CreateIndex(
                name: "IX_trust_score_rules_rule_type",
                table: "trust_score_rules",
                column: "rule_type",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_trust_score_rules_rule_type",
                table: "trust_score_rules");

            migrationBuilder.AddColumn<DateTime>(
                name: "effective_from",
                table: "trust_score_rules",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "effective_to",
                table: "trust_score_rules",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_trust_score_rule_period",
                table: "trust_score_rules",
                sql: "\"effective_to\" IS NULL OR \"effective_to\" > \"effective_from\"");
        }
    }
}
