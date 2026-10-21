using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaveBite.Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTrustLevelDepositPolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "trust_level_deposit_policies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    trust_level_id = table.Column<Guid>(type: "uuid", nullable: false),
                    require_full_online_payment = table.Column<bool>(type: "boolean", nullable: false),
                    deposit_free_order_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    max_deposit_order_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trust_level_deposit_policies", x => x.id);
                    table.CheckConstraint("ck_deposit_policy_payment_mode", "(require_full_online_payment AND deposit_free_order_amount IS NULL AND max_deposit_order_amount IS NULL) OR (NOT require_full_online_payment AND deposit_free_order_amount IS NOT NULL AND max_deposit_order_amount IS NOT NULL AND deposit_free_order_amount >= 0 AND max_deposit_order_amount > deposit_free_order_amount)");
                    table.ForeignKey(
                        name: "FK_trust_level_deposit_policies_trust_levels_trust_level_id",
                        column: x => x.trust_level_id,
                        principalTable: "trust_levels",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "trust_level_deposit_ranges",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    deposit_policy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_order_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    to_order_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    deposit_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trust_level_deposit_ranges", x => x.id);
                    table.CheckConstraint("ck_deposit_range_amount", "deposit_amount > 0 AND deposit_amount <= from_order_amount");
                    table.CheckConstraint("ck_deposit_range_bounds", "from_order_amount >= 0 AND to_order_amount > from_order_amount");
                    table.ForeignKey(
                        name: "FK_trust_level_deposit_ranges_trust_level_deposit_policies_dep~",
                        column: x => x.deposit_policy_id,
                        principalTable: "trust_level_deposit_policies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_trust_level_deposit_policies_trust_level_id",
                table: "trust_level_deposit_policies",
                column: "trust_level_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_trust_level_deposit_ranges_deposit_policy_id_from_order_amo~",
                table: "trust_level_deposit_ranges",
                columns: new[] { "deposit_policy_id", "from_order_amount" },
                unique: true);
            // Validate the final configuration at transaction commit so ranges can be edited together.
            migrationBuilder.Sql("""
                CREATE FUNCTION validate_trust_level_deposit_policy() RETURNS trigger
                LANGUAGE plpgsql AS $$
                DECLARE
                    policy_id uuid;
                    policy trust_level_deposit_policies%ROWTYPE;
                    policy_ids uuid[];
                BEGIN
                    IF TG_TABLE_NAME = 'trust_level_deposit_policies' THEN
                        IF TG_OP = 'DELETE' THEN policy_ids := ARRAY[OLD.id];
                        ELSE policy_ids := ARRAY[NEW.id]; END IF;
                    ELSE
                        IF TG_OP = 'INSERT' THEN policy_ids := ARRAY[NEW.deposit_policy_id];
                        ELSIF TG_OP = 'DELETE' THEN policy_ids := ARRAY[OLD.deposit_policy_id];
                        ELSE policy_ids := ARRAY[OLD.deposit_policy_id, NEW.deposit_policy_id]; END IF;
                    END IF;
                    FOREACH policy_id IN ARRAY policy_ids LOOP
                        SELECT * INTO policy FROM trust_level_deposit_policies WHERE id = policy_id FOR UPDATE;
                        IF NOT FOUND THEN CONTINUE; END IF;
                        IF policy.require_full_online_payment THEN
                            IF EXISTS (SELECT 1 FROM trust_level_deposit_ranges WHERE deposit_policy_id = policy_id) THEN
                                RAISE EXCEPTION 'Full-online-payment policy cannot have deposit ranges' USING ERRCODE = '23514';
                            END IF;
                        ELSE
                            IF NOT EXISTS (SELECT 1 FROM trust_level_deposit_ranges WHERE deposit_policy_id = policy_id) OR
                               EXISTS (
                                   SELECT 1 FROM (
                                       SELECT from_order_amount, to_order_amount,
                                           lag(to_order_amount, 1, policy.deposit_free_order_amount)
                                               OVER (ORDER BY from_order_amount) AS previous_end
                                       FROM trust_level_deposit_ranges WHERE deposit_policy_id = policy_id
                                   ) ranges
                                   WHERE from_order_amount <> previous_end OR to_order_amount > policy.max_deposit_order_amount
                               ) OR
                               (SELECT max(to_order_amount) FROM trust_level_deposit_ranges WHERE deposit_policy_id = policy_id)
                                   <> policy.max_deposit_order_amount THEN
                                RAISE EXCEPTION 'Deposit ranges must cover the policy interval without gaps or overlaps' USING ERRCODE = '23514';
                            END IF;
                        END IF;
                    END LOOP;
                    RETURN NULL;
                END;
                $$;
                CREATE CONSTRAINT TRIGGER check_deposit_policy_ranges
                AFTER INSERT OR UPDATE ON trust_level_deposit_policies
                DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION validate_trust_level_deposit_policy();
                CREATE CONSTRAINT TRIGGER check_deposit_ranges_policy
                AFTER INSERT OR UPDATE OR DELETE ON trust_level_deposit_ranges
                DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION validate_trust_level_deposit_policy();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER check_deposit_policy_ranges ON trust_level_deposit_policies;");
            migrationBuilder.Sql("DROP TRIGGER check_deposit_ranges_policy ON trust_level_deposit_ranges;");
            migrationBuilder.Sql("DROP FUNCTION validate_trust_level_deposit_policy();");
            migrationBuilder.DropTable(
                name: "trust_level_deposit_ranges");

            migrationBuilder.DropTable(
                name: "trust_level_deposit_policies");

        }
    }
}
