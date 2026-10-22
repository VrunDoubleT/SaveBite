using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaveBite.Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderTrustProcessing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "rule_type",
                table: "trust_score_rules",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "OnlineOrderCompleted");

            // Preserve historical rule versions and start the two new earning schedules at deployment time.
            migrationBuilder.Sql("""
                UPDATE trust_score_rules SET rule_type = CASE WHEN score_delta > 0 THEN 'OnlineOrderCompleted' ELSE 'NoShow' END;
                DO $$
                DECLARE actor uuid; start_at timestamptz := CURRENT_TIMESTAMP;
                BEGIN
                    SELECT created_by INTO actor FROM trust_score_rules ORDER BY created_at LIMIT 1;
                    IF actor IS NOT NULL THEN
                        UPDATE trust_score_rules SET effective_to = start_at
                        WHERE score_delta > 0 AND effective_from < start_at AND (effective_to IS NULL OR effective_to > start_at);
                        -- Keep future administrator schedules; the new default stops when the next online version begins.
                        INSERT INTO trust_score_rules(id, rule_type, score_delta, effective_from, effective_to, created_by, created_at)
                        VALUES (gen_random_uuid(), 'OnlineOrderCompleted', 3, start_at,
                            (SELECT min(effective_from) FROM trust_score_rules WHERE score_delta > 0 AND effective_from > start_at), actor, start_at);
                        INSERT INTO trust_score_rules(id, rule_type, score_delta, effective_from, created_by, created_at)
                        VALUES (gen_random_uuid(), 'PayAtShopOrderCompleted', 4, start_at, actor, start_at);
                    END IF;
                END $$;
                """);

            migrationBuilder.CreateTable(
                name: "trust_order_processing",
                columns: table => new
                {
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trust_order_processing", x => x.order_id);
                    table.ForeignKey(
                        name: "FK_trust_order_processing_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id");
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "trust_order_processing");

            migrationBuilder.DropColumn(
                name: "rule_type",
                table: "trust_score_rules");
        }
    }
}
