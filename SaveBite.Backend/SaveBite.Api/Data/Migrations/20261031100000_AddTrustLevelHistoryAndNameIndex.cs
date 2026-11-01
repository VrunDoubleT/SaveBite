using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SaveBite.Backend.Data;

#nullable disable

namespace SaveBite.Backend.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261031100000_AddTrustLevelHistoryAndNameIndex")]
public partial class AddTrustLevelHistoryAndNameIndex : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "trust_level_histories",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                trust_level_id = table.Column<Guid>(type: "uuid", nullable: false),
                version_no = table.Column<int>(type: "integer", nullable: false),
                change_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                description = table.Column<string>(type: "text", nullable: true),
                rank = table.Column<int>(type: "integer", nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                min_score = table.Column<int>(type: "integer", nullable: false),
                min_completed_orders = table.Column<int>(type: "integer", nullable: false),
                min_total_order_value = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                deposit_policy_snapshot = table.Column<string>(type: "jsonb", nullable: true),
                affected_user_count = table.Column<int>(type: "integer", nullable: false),
                changed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_trust_level_histories", x => x.id);
                table.CheckConstraint("ck_trust_level_history_version", "\"version_no\" >= 1");
                table.CheckConstraint("ck_trust_level_history_affected_users", "\"affected_user_count\" >= 0");
                table.ForeignKey(
                    name: "FK_trust_level_histories_trust_levels_trust_level_id",
                    column: x => x.trust_level_id,
                    principalTable: "trust_levels",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_trust_level_histories_users_changed_by_user_id",
                    column: x => x.changed_by_user_id,
                    principalTable: "users",
                    principalColumn: "id");
            });

        migrationBuilder.CreateIndex(
            name: "ux_trust_level_histories_level_version",
            table: "trust_level_histories",
            columns: new[] { "trust_level_id", "version_no" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_trust_level_histories_level_changed_at",
            table: "trust_level_histories",
            columns: new[] { "trust_level_id", "changed_at" });

        migrationBuilder.CreateIndex(
            name: "IX_trust_level_histories_changed_by_user_id",
            table: "trust_level_histories",
            column: "changed_by_user_id");

        // Unique index on lower(name) for case-insensitive uniqueness across all levels.
        migrationBuilder.Sql("""
            CREATE UNIQUE INDEX IF NOT EXISTS ix_trust_levels_name_lower
            ON trust_levels (lower(name));
            """);

        // Seed version 1 history rows for any existing levels so history is never empty.
        migrationBuilder.Sql("""
            INSERT INTO trust_level_histories (
                id,
                trust_level_id,
                version_no,
                change_type,
                name,
                description,
                rank,
                is_active,
                min_score,
                min_completed_orders,
                min_total_order_value,
                deposit_policy_snapshot,
                affected_user_count,
                changed_by_user_id,
                changed_at
            )
            SELECT
                gen_random_uuid(),
                tl.id,
                1,
                'Created',
                tl.name,
                tl.description,
                tl.rank,
                tl.is_active,
                COALESCE(req.min_score, 0),
                COALESCE(req.min_completed_orders, 0),
                COALESCE(req.min_total_order_value, 0),
                CASE
                    WHEN dp.id IS NULL THEN NULL
                    ELSE jsonb_build_object(
                        'RequireFullOnlinePayment', dp.require_full_online_payment,
                        'DepositFreeOrderAmount', dp.deposit_free_order_amount,
                        'MaxDepositOrderAmount', dp.max_deposit_order_amount,
                        'Ranges', COALESCE((
                            SELECT jsonb_agg(
                                jsonb_build_object(
                                    'FromOrderAmount', r.from_order_amount,
                                    'ToOrderAmount', r.to_order_amount,
                                    'DepositAmount', r.deposit_amount
                                ) ORDER BY r.from_order_amount
                            )
                            FROM trust_level_deposit_ranges r
                            WHERE r.deposit_policy_id = dp.id
                        ), '[]'::jsonb)
                    )
                END,
                COALESCE((
                    SELECT COUNT(*)::int
                    FROM user_trust_scores uts
                    WHERE uts.trust_level_id = tl.id
                ), 0),
                (SELECT u.id FROM users u WHERE u.role = 'Admin' ORDER BY u.created_at LIMIT 1),
                tl.created_at
            FROM trust_levels tl
            LEFT JOIN trust_level_requirements req ON req.trust_level_id = tl.id
            LEFT JOIN trust_level_deposit_policies dp ON dp.trust_level_id = tl.id
            WHERE NOT EXISTS (
                SELECT 1 FROM trust_level_histories h WHERE h.trust_level_id = tl.id
            );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP INDEX IF EXISTS ix_trust_levels_name_lower;");
        migrationBuilder.DropTable(name: "trust_level_histories");
    }
}