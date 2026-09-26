using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SaveBite.Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    image_url = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "trust_levels",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    rank = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trust_levels", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    password_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    full_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    avatar_url = table.Column<string>(type: "text", nullable: true),
                    email_verified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    customer_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "Active"),
                    shop_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "Active"),
                    role = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "trust_level_requirements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    trust_level_id = table.Column<Guid>(type: "uuid", nullable: false),
                    min_score = table.Column<int>(type: "integer", nullable: false),
                    min_completed_orders = table.Column<int>(type: "integer", nullable: false),
                    min_total_order_value = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trust_level_requirements", x => x.id);
                    table.ForeignKey(
                        name: "FK_trust_level_requirements_trust_levels_trust_level_id",
                        column: x => x.trust_level_id,
                        principalTable: "trust_levels",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "account_status_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    admin_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    reason = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_account_status_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_account_status_logs_users_admin_id",
                        column: x => x.admin_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_account_status_logs_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    actor_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    target_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    target_id = table.Column<Guid>(type: "uuid", nullable: false),
                    old_values = table.Column<string>(type: "jsonb", nullable: true),
                    new_values = table.Column<string>(type: "jsonb", nullable: true),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.id);
                    table.CheckConstraint("audit_logs_actor_check", "(\"actor_type\" = 'User' AND \"actor_user_id\" IS NOT NULL) OR (\"actor_type\" = 'System' AND \"actor_user_id\" IS NULL)");
                    table.ForeignKey(
                        name: "FK_audit_logs_users_actor_user_id",
                        column: x => x.actor_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    body = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    action_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    deduplication_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    read_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    dismissed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notifications", x => x.id);
                    table.ForeignKey(
                        name: "FK_notifications_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "platform_fee_configs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    fee_rate = table.Column<decimal>(type: "numeric(7,6)", precision: 7, scale: 6, nullable: false),
                    effective_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    effective_to = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_platform_fee_configs", x => x.id);
                    table.CheckConstraint("ck_platform_fee_effective_period", "\"effective_to\" IS NULL OR \"effective_to\" > \"effective_from\"");
                    table.CheckConstraint("ck_platform_fee_rate", "\"fee_rate\" BETWEEN 0 AND 1");
                    table.ForeignKey(
                        name: "FK_platform_fee_configs_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "recovery_requirement_rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    minimum_occurrence = table.Column<int>(type: "integer", nullable: false),
                    score_deduction = table.Column<int>(type: "integer", nullable: false),
                    required_online_orders = table.Column<int>(type: "integer", nullable: false),
                    required_online_order_value = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    permanent_offline_lock = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    effective_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    effective_to = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recovery_requirement_rules", x => x.id);
                    table.CheckConstraint("ck_recovery_requirement_rule_occurrence", "\"minimum_occurrence\" > 0");
                    table.CheckConstraint("ck_recovery_requirement_rule_period", "\"effective_to\" IS NULL OR \"effective_to\" > \"effective_from\"");
                    table.CheckConstraint("ck_recovery_requirement_rule_requirements", "\"required_online_orders\" >= 0 AND \"required_online_order_value\" >= 0");
                    table.CheckConstraint("ck_recovery_requirement_rule_score_deduction", "\"score_deduction\" > 0");
                    table.ForeignKey(
                        name: "FK_recovery_requirement_rules_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    revoke_reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    replaced_by_token_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "FK_refresh_tokens_refresh_tokens_replaced_by_token_id",
                        column: x => x.replaced_by_token_id,
                        principalTable: "refresh_tokens",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_refresh_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "shop_applications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    applicant_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    business_license_no = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    address_line = table.Column<string>(type: "text", nullable: false),
                    ward = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    district = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    latitude = table.Column<double>(type: "double precision", nullable: false),
                    longitude = table.Column<double>(type: "double precision", nullable: false),
                    logo_url = table.Column<string>(type: "text", nullable: true),
                    cover_image_url = table.Column<string>(type: "text", nullable: true),
                    opening_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    closing_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    bank_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    bank_account_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    bank_account_holder = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    payos_client_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    payos_api_key = table.Column<string>(type: "text", nullable: false),
                    payos_checksum_key = table.Column<string>(type: "text", nullable: false),
                    revision_number = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shop_applications", x => x.id);
                    table.CheckConstraint("ck_shop_application_revision", "\"revision_number\" > 0");
                    table.ForeignKey(
                        name: "FK_shop_applications_users_applicant_user_id",
                        column: x => x.applicant_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "trust_score_rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    score_delta = table.Column<int>(type: "integer", nullable: false),
                    effective_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    effective_to = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trust_score_rules", x => x.id);
                    table.CheckConstraint("ck_trust_score_rule_period", "\"effective_to\" IS NULL OR \"effective_to\" > \"effective_from\"");
                    table.ForeignKey(
                        name: "FK_trust_score_rules_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "user_addresses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    label = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    address_line = table.Column<string>(type: "text", nullable: false),
                    ward = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    district = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    latitude = table.Column<double>(type: "double precision", nullable: false),
                    longitude = table.Column<double>(type: "double precision", nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_addresses", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_addresses_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "user_level_progress",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    completed_orders_count = table.Column<int>(type: "integer", nullable: false),
                    accumulated_order_value = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_level_progress", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_level_progress_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "user_role_change_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    previous_role = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    new_role = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_role_change_logs", x => x.id);
                    table.CheckConstraint("user_role_change_logs_roles_different_check", "\"previous_role\" <> \"new_role\"");
                    table.ForeignKey(
                        name: "FK_user_role_change_logs_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_trust_scores",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    trust_level_id = table.Column<Guid>(type: "uuid", nullable: false),
                    current_score = table.Column<int>(type: "integer", nullable: false),
                    is_offline_locked_permanently = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_trust_scores", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_trust_scores_trust_levels_trust_level_id",
                        column: x => x.trust_level_id,
                        principalTable: "trust_levels",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_user_trust_scores_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "shop_application_documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    application_id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    file_url = table.Column<string>(type: "text", nullable: false),
                    original_file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    revision_number = table.Column<int>(type: "integer", nullable: false),
                    uploaded_by = table.Column<Guid>(type: "uuid", nullable: false),
                    uploaded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_current = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shop_application_documents", x => x.id);
                    table.CheckConstraint("ck_shop_application_document_revision", "\"revision_number\" > 0");
                    table.ForeignKey(
                        name: "FK_shop_application_documents_shop_applications_application_id",
                        column: x => x.application_id,
                        principalTable: "shop_applications",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_shop_application_documents_users_uploaded_by",
                        column: x => x.uploaded_by,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "shop_application_review_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    application_id = table.Column<Guid>(type: "uuid", nullable: false),
                    admin_id = table.Column<Guid>(type: "uuid", nullable: true),
                    from_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    to_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    revision_number = table.Column<int>(type: "integer", nullable: false),
                    note = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shop_application_review_logs", x => x.id);
                    table.CheckConstraint("ck_shop_application_review_revision", "\"revision_number\" > 0");
                    table.ForeignKey(
                        name: "FK_shop_application_review_logs_shop_applications_application_~",
                        column: x => x.application_id,
                        principalTable: "shop_applications",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_shop_application_review_logs_users_admin_id",
                        column: x => x.admin_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "shops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    application_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    address_line = table.Column<string>(type: "text", nullable: false),
                    ward = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    district = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    latitude = table.Column<double>(type: "double precision", nullable: false),
                    longitude = table.Column<double>(type: "double precision", nullable: false),
                    logo_url = table.Column<string>(type: "text", nullable: true),
                    cover_image_url = table.Column<string>(type: "text", nullable: true),
                    opening_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    closing_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shops", x => x.id);
                    table.ForeignKey(
                        name: "FK_shops_shop_applications_application_id",
                        column: x => x.application_id,
                        principalTable: "shop_applications",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_shops_users_owner_user_id",
                        column: x => x.owner_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    order_code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shop_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_method = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    deposit_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    remaining_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    distance_km = table.Column<decimal>(type: "numeric(8,3)", precision: 8, scale: 3, nullable: true),
                    eta_minutes = table.Column<int>(type: "integer", nullable: true),
                    soft_deadline_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    hard_deadline_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_late_arrival = table.Column<bool>(type: "boolean", nullable: false),
                    no_show = table.Column<bool>(type: "boolean", nullable: false),
                    cancel_reason = table.Column<string>(type: "text", nullable: true),
                    cancelled_by = table.Column<Guid>(type: "uuid", nullable: true),
                    confirmed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ready_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    expired_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orders", x => x.id);
                    table.ForeignKey(
                        name: "FK_orders_shops_shop_id",
                        column: x => x.shop_id,
                        principalTable: "shops",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_orders_users_cancelled_by",
                        column: x => x.cancelled_by,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_orders_users_customer_id",
                        column: x => x.customer_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "platform_fee_statements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    shop_id = table.Column<Guid>(type: "uuid", nullable: false),
                    billing_year = table.Column<int>(type: "integer", nullable: false),
                    billing_month = table.Column<short>(type: "smallint", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    total_fee_amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false, defaultValue: 0m),
                    issued_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    due_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    paid_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_platform_fee_statements", x => x.id);
                    table.CheckConstraint("ck_platform_fee_statement_month", "\"billing_month\" BETWEEN 1 AND 12");
                    table.CheckConstraint("ck_platform_fee_statement_total", "\"total_fee_amount\" >= 0");
                    table.ForeignKey(
                        name: "FK_platform_fee_statements_shops_shop_id",
                        column: x => x.shop_id,
                        principalTable: "shops",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    shop_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.id);
                    table.ForeignKey(
                        name: "FK_Products_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "categories",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_Products_shops_shop_id",
                        column: x => x.shop_id,
                        principalTable: "shops",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "shop_payment_configs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    shop_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bank_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    bank_account_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    bank_account_holder = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    payos_client_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    payos_api_key = table.Column<string>(type: "text", nullable: false),
                    payos_checksum_key = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shop_payment_configs", x => x.id);
                    table.ForeignKey(
                        name: "FK_shop_payment_configs_shops_shop_id",
                        column: x => x.shop_id,
                        principalTable: "shops",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "shop_staff",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    shop_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    nickname = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    joined_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shop_staff", x => x.id);
                    table.ForeignKey(
                        name: "FK_shop_staff_shops_shop_id",
                        column: x => x.shop_id,
                        principalTable: "shops",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_shop_staff_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "staff_invitations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    shop_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invited_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invited_by = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    invited_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    responded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_staff_invitations", x => x.id);
                    table.ForeignKey(
                        name: "FK_staff_invitations_shops_shop_id",
                        column: x => x.shop_id,
                        principalTable: "shops",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_staff_invitations_users_invited_by",
                        column: x => x.invited_by,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_staff_invitations_users_invited_user_id",
                        column: x => x.invited_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "no_show_penalties",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recovery_rule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    penalty_year = table.Column<int>(type: "integer", nullable: false),
                    penalty_month = table.Column<int>(type: "integer", nullable: false),
                    occurrence_number = table.Column<int>(type: "integer", nullable: false),
                    score_deducted = table.Column<int>(type: "integer", nullable: false),
                    required_online_orders = table.Column<int>(type: "integer", nullable: false),
                    required_online_order_value = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    completed_online_orders = table.Column<int>(type: "integer", nullable: false),
                    completed_online_order_value = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    permanent_offline_lock = table.Column<bool>(type: "boolean", nullable: false),
                    lock_ends_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    recovered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_no_show_penalties", x => x.id);
                    table.ForeignKey(
                        name: "FK_no_show_penalties_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_no_show_penalties_recovery_requirement_rules_recovery_rule_~",
                        column: x => x.recovery_rule_id,
                        principalTable: "recovery_requirement_rules",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_no_show_penalties_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "order_status_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    to_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    changed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    note = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_status_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_order_status_history_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_order_status_history_users_changed_by",
                        column: x => x.changed_by,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    method = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    gateway_transaction_id = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    paid_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payments", x => x.id);
                    table.ForeignKey(
                        name: "FK_payments_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "refunds",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    refund_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    initiated_by = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: true),
                    requested_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    receiver_method = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    receiver_bank_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    receiver_account_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    receiver_account_holder = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    receiver_qr_image_url = table.Column<string>(type: "text", nullable: true),
                    reviewed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    reject_reason = table.Column<string>(type: "text", nullable: true),
                    transfer_reference = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    transferred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refunds", x => x.id);
                    table.CheckConstraint("ck_refunds_requested_amount", "\"requested_amount\" > 0");
                    table.CheckConstraint("ck_refunds_shop_cancellation_not_rejected", "\"refund_type\" <> 'ShopCancellation' OR \"status\" <> 'Rejected'");
                    table.ForeignKey(
                        name: "FK_refunds_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_refunds_users_initiated_by",
                        column: x => x.initiated_by,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_refunds_users_reviewed_by",
                        column: x => x.reviewed_by,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "staff_activity_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    shop_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action_by = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    note = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_staff_activity_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_staff_activity_logs_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_staff_activity_logs_shops_shop_id",
                        column: x => x.shop_id,
                        principalTable: "shops",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_staff_activity_logs_users_action_by",
                        column: x => x.action_by,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "order_platform_fees",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shop_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fee_config_id = table.Column<Guid>(type: "uuid", nullable: false),
                    statement_id = table.Column<Guid>(type: "uuid", nullable: true),
                    gross_amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    fee_rate = table.Column<decimal>(type: "numeric(7,6)", precision: 7, scale: 6, nullable: false),
                    fee_amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    calculated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_platform_fees", x => x.id);
                    table.CheckConstraint("ck_order_platform_fee_amount", "\"fee_amount\" >= 0");
                    table.CheckConstraint("ck_order_platform_fee_gross", "\"gross_amount\" >= 0");
                    table.CheckConstraint("ck_order_platform_fee_rate", "\"fee_rate\" BETWEEN 0 AND 1");
                    table.ForeignKey(
                        name: "FK_order_platform_fees_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_order_platform_fees_platform_fee_configs_fee_config_id",
                        column: x => x.fee_config_id,
                        principalTable: "platform_fee_configs",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_order_platform_fees_platform_fee_statements_statement_id",
                        column: x => x.statement_id,
                        principalTable: "platform_fee_statements",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_order_platform_fees_shops_shop_id",
                        column: x => x.shop_id,
                        principalTable: "shops",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "flash_deals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    shop_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sale_start_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    order_end_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    shop_closing_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_flash_deals", x => x.id);
                    table.ForeignKey(
                        name: "FK_flash_deals_Products_product_id",
                        column: x => x.product_id,
                        principalTable: "Products",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_flash_deals_shops_shop_id",
                        column: x => x.shop_id,
                        principalTable: "shops",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "product_attributes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_attributes", x => x.id);
                    table.ForeignKey(
                        name: "FK_product_attributes_Products_product_id",
                        column: x => x.product_id,
                        principalTable: "Products",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "product_content_revisions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    revision_number = table.Column<int>(type: "integer", nullable: false),
                    submitted_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    images_json = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    reviewed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    review_note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_content_revisions", x => x.id);
                    table.CheckConstraint("product_content_revisions_number_check", "\"revision_number\" >= 1");
                    table.CheckConstraint("product_content_revisions_review_check", "(\"status\" = 'Pending' AND \"reviewed_by_user_id\" IS NULL AND \"reviewed_at\" IS NULL) OR (\"status\" IN ('Approved', 'NeedsRevision', 'Suspended') AND \"reviewed_by_user_id\" IS NOT NULL AND \"reviewed_at\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_product_content_revisions_Products_product_id",
                        column: x => x.product_id,
                        principalTable: "Products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_product_content_revisions_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_product_content_revisions_users_reviewed_by_user_id",
                        column: x => x.reviewed_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_product_content_revisions_users_submitted_by_user_id",
                        column: x => x.submitted_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_images",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    image_url = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_images", x => x.id);
                    table.ForeignKey(
                        name: "FK_product_images_Products_product_id",
                        column: x => x.product_id,
                        principalTable: "Products",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "product_variants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sku = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    original_price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    deal_price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_variants", x => x.id);
                    table.CheckConstraint("ck_product_variants_prices", "\"original_price\" > 0 AND \"deal_price\" >= 0 AND \"deal_price\" < \"original_price\"");
                    table.ForeignKey(
                        name: "FK_product_variants_Products_product_id",
                        column: x => x.product_id,
                        principalTable: "Products",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "no_show_penalty_order_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    no_show_penalty_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_value = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_no_show_penalty_order_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_no_show_penalty_order_history_no_show_penalties_no_show_pen~",
                        column: x => x.no_show_penalty_id,
                        principalTable: "no_show_penalties",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_no_show_penalty_order_history_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "trust_score_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    rule_id = table.Column<Guid>(type: "uuid", nullable: true),
                    no_show_penalty_id = table.Column<Guid>(type: "uuid", nullable: true),
                    event_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    score_before = table.Column<int>(type: "integer", nullable: false),
                    score_delta = table.Column<int>(type: "integer", nullable: false),
                    score_after = table.Column<int>(type: "integer", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trust_score_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_trust_score_history_no_show_penalties_no_show_penalty_id",
                        column: x => x.no_show_penalty_id,
                        principalTable: "no_show_penalties",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_trust_score_history_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_trust_score_history_trust_score_rules_rule_id",
                        column: x => x.rule_id,
                        principalTable: "trust_score_rules",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_trust_score_history_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "user_trust_level_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    previous_trust_level_id = table.Column<Guid>(type: "uuid", nullable: true),
                    new_trust_level_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    no_show_penalty_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_trust_level_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_trust_level_history_no_show_penalties_no_show_penalty_~",
                        column: x => x.no_show_penalty_id,
                        principalTable: "no_show_penalties",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_user_trust_level_history_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_user_trust_level_history_trust_levels_new_trust_level_id",
                        column: x => x.new_trust_level_id,
                        principalTable: "trust_levels",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_user_trust_level_history_trust_levels_previous_trust_level_~",
                        column: x => x.previous_trust_level_id,
                        principalTable: "trust_levels",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_user_trust_level_history_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "refund_evidence_images",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    refund_id = table.Column<Guid>(type: "uuid", nullable: false),
                    image_url = table.Column<string>(type: "text", nullable: false),
                    image_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    uploaded_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refund_evidence_images", x => x.id);
                    table.ForeignKey(
                        name: "FK_refund_evidence_images_refunds_refund_id",
                        column: x => x.refund_id,
                        principalTable: "refunds",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_refund_evidence_images_users_uploaded_by",
                        column: x => x.uploaded_by,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "platform_fee_adjustments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    shop_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_fee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    refund_id = table.Column<Guid>(type: "uuid", nullable: false),
                    statement_id = table.Column<Guid>(type: "uuid", nullable: true),
                    amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    reason = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_platform_fee_adjustments", x => x.id);
                    table.CheckConstraint("ck_platform_fee_adjustment_negative", "\"amount\" < 0");
                    table.ForeignKey(
                        name: "FK_platform_fee_adjustments_order_platform_fees_order_fee_id",
                        column: x => x.order_fee_id,
                        principalTable: "order_platform_fees",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_platform_fee_adjustments_platform_fee_statements_statement_~",
                        column: x => x.statement_id,
                        principalTable: "platform_fee_statements",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_platform_fee_adjustments_refunds_refund_id",
                        column: x => x.refund_id,
                        principalTable: "refunds",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_platform_fee_adjustments_shops_shop_id",
                        column: x => x.shop_id,
                        principalTable: "shops",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "product_attribute_values",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    attribute_id = table.Column<Guid>(type: "uuid", nullable: false),
                    value = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    image_url = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_attribute_values", x => x.id);
                    table.ForeignKey(
                        name: "FK_product_attribute_values_product_attributes_attribute_id",
                        column: x => x.attribute_id,
                        principalTable: "product_attributes",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "flash_deal_variants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    flash_deal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    variant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    original_price = table.Column<decimal>(type: "numeric", nullable: false),
                    deal_price = table.Column<decimal>(type: "numeric", nullable: false),
                    discount_percent = table.Column<decimal>(type: "numeric", nullable: false),
                    total_quantity = table.Column<int>(type: "integer", nullable: false),
                    reserved_quantity = table.Column<int>(type: "integer", nullable: false),
                    sold_quantity = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_flash_deal_variants", x => x.id);
                    table.CheckConstraint("flash_deal_variant_stock_valid", "\"total_quantity\" >= 0 AND \"reserved_quantity\" >= 0 AND \"sold_quantity\" >= 0 AND \"reserved_quantity\" + \"sold_quantity\" <= \"total_quantity\"");
                    table.ForeignKey(
                        name: "FK_flash_deal_variants_flash_deals_flash_deal_id",
                        column: x => x.flash_deal_id,
                        principalTable: "flash_deals",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_flash_deal_variants_product_variants_variant_id",
                        column: x => x.variant_id,
                        principalTable: "product_variants",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "product_variant_values",
                columns: table => new
                {
                    variant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    attribute_value_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_variant_values", x => new { x.variant_id, x.attribute_value_id });
                    table.ForeignKey(
                        name: "FK_product_variant_values_product_attribute_values_attribute_v~",
                        column: x => x.attribute_value_id,
                        principalTable: "product_attribute_values",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_product_variant_values_product_variants_variant_id",
                        column: x => x.variant_id,
                        principalTable: "product_variants",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "cart_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    flash_deal_variant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    added_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cart_items", x => x.id);
                    table.CheckConstraint("cart_items_positive_quantity", "\"quantity\" > 0");
                    table.ForeignKey(
                        name: "FK_cart_items_flash_deal_variants_flash_deal_variant_id",
                        column: x => x.flash_deal_variant_id,
                        principalTable: "flash_deal_variants",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_cart_items_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "order_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    flash_deal_variant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_name_snapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    variant_name_snapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    original_price_snapshot = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    deal_price_snapshot = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_items", x => x.id);
                    table.CheckConstraint("order_items_valid", "\"unit_price\" >= 0 AND \"quantity\" > 0 AND (\"subtotal\" IS NULL OR \"subtotal\" >= 0)");
                    table.ForeignKey(
                        name: "FK_order_items_flash_deal_variants_flash_deal_variant_id",
                        column: x => x.flash_deal_variant_id,
                        principalTable: "flash_deal_variants",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_order_items_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "product_feedbacks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    order_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rating = table.Column<int>(type: "integer", nullable: false),
                    comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    hidden_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_feedbacks", x => x.id);
                    table.CheckConstraint("product_feedbacks_hidden_at_check", "(\"status\" = 'Visible' AND \"hidden_at\" IS NULL) OR (\"status\" <> 'Visible' AND \"hidden_at\" IS NOT NULL)");
                    table.CheckConstraint("product_feedbacks_rating_check", "\"rating\" BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "FK_product_feedbacks_Products_product_id",
                        column: x => x.product_id,
                        principalTable: "Products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_product_feedbacks_order_items_order_item_id",
                        column: x => x.order_item_id,
                        principalTable: "order_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_product_feedbacks_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_feedback_moderation_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    product_feedback_id = table.Column<Guid>(type: "uuid", nullable: false),
                    admin_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    previous_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    new_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_feedback_moderation_logs", x => x.id);
                    table.CheckConstraint("product_feedback_moderation_logs_status_change_check", "\"previous_status\" <> \"new_status\"");
                    table.ForeignKey(
                        name: "FK_product_feedback_moderation_logs_product_feedbacks_product_~",
                        column: x => x.product_feedback_id,
                        principalTable: "product_feedbacks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_product_feedback_moderation_logs_users_admin_user_id",
                        column: x => x.admin_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_feedback_replies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    product_feedback_id = table.Column<Guid>(type: "uuid", nullable: false),
                    replied_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    content = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_feedback_replies", x => x.id);
                    table.ForeignKey(
                        name: "FK_product_feedback_replies_product_feedbacks_product_feedback~",
                        column: x => x.product_feedback_id,
                        principalTable: "product_feedbacks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_product_feedback_replies_users_replied_by_user_id",
                        column: x => x.replied_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_account_status_logs_admin_id",
                table: "account_status_logs",
                column: "admin_id");

            migrationBuilder.CreateIndex(
                name: "IX_account_status_logs_user_id",
                table: "account_status_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_action_created_at",
                table: "audit_logs",
                columns: new[] { "action", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_actor_user_id_created_at",
                table: "audit_logs",
                columns: new[] { "actor_user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_correlation_id",
                table: "audit_logs",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_target_type_target_id_created_at",
                table: "audit_logs",
                columns: new[] { "target_type", "target_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "cart_items_unique_0",
                table: "cart_items",
                columns: new[] { "user_id", "flash_deal_variant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cart_items_flash_deal_variant_id",
                table: "cart_items",
                column: "flash_deal_variant_id");

            migrationBuilder.CreateIndex(
                name: "IX_flash_deal_variants_variant_id",
                table: "flash_deal_variants",
                column: "variant_id");

            migrationBuilder.CreateIndex(
                name: "uq_deal_variant",
                table: "flash_deal_variants",
                columns: new[] { "flash_deal_id", "variant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_flash_deals_product_id",
                table: "flash_deals",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_flash_deals_shop_id",
                table: "flash_deals",
                column: "shop_id");

            migrationBuilder.CreateIndex(
                name: "IX_no_show_penalties_order_id",
                table: "no_show_penalties",
                column: "order_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_no_show_penalties_recovery_rule_id",
                table: "no_show_penalties",
                column: "recovery_rule_id");

            migrationBuilder.CreateIndex(
                name: "IX_no_show_penalties_user_id",
                table: "no_show_penalties",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_no_show_penalty_order_history_no_show_penalty_id",
                table: "no_show_penalty_order_history",
                column: "no_show_penalty_id");

            migrationBuilder.CreateIndex(
                name: "IX_no_show_penalty_order_history_order_id",
                table: "no_show_penalty_order_history",
                column: "order_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notifications_unread_user",
                table: "notifications",
                column: "user_id",
                filter: "\"read_at\" IS NULL AND \"dismissed_at\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_user_id_created_at",
                table: "notifications",
                columns: new[] { "user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_notifications_user_id_deduplication_key",
                table: "notifications",
                columns: new[] { "user_id", "deduplication_key" },
                unique: true,
                filter: "\"deduplication_key\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_order_items_flash_deal_variant_id",
                table: "order_items",
                column: "flash_deal_variant_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_items_order_id",
                table: "order_items",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_platform_fees_fee_config_id",
                table: "order_platform_fees",
                column: "fee_config_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_platform_fees_order_id",
                table: "order_platform_fees",
                column: "order_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_order_platform_fees_shop_id",
                table: "order_platform_fees",
                column: "shop_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_platform_fees_statement_id",
                table: "order_platform_fees",
                column: "statement_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_status_history_changed_by",
                table: "order_status_history",
                column: "changed_by");

            migrationBuilder.CreateIndex(
                name: "IX_order_status_history_order_id",
                table: "order_status_history",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_orders_cancelled_by",
                table: "orders",
                column: "cancelled_by");

            migrationBuilder.CreateIndex(
                name: "IX_orders_customer_id",
                table: "orders",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_orders_order_code",
                table: "orders",
                column: "order_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_orders_shop_id",
                table: "orders",
                column: "shop_id");

            migrationBuilder.CreateIndex(
                name: "IX_payments_order_id",
                table: "payments",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_platform_fee_adjustments_order_fee_id",
                table: "platform_fee_adjustments",
                column: "order_fee_id");

            migrationBuilder.CreateIndex(
                name: "IX_platform_fee_adjustments_refund_id",
                table: "platform_fee_adjustments",
                column: "refund_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_platform_fee_adjustments_shop_id",
                table: "platform_fee_adjustments",
                column: "shop_id");

            migrationBuilder.CreateIndex(
                name: "IX_platform_fee_adjustments_statement_id",
                table: "platform_fee_adjustments",
                column: "statement_id");

            migrationBuilder.CreateIndex(
                name: "IX_platform_fee_configs_created_by",
                table: "platform_fee_configs",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_platform_fee_statements_shop_id_billing_year_billing_month",
                table: "platform_fee_statements",
                columns: new[] { "shop_id", "billing_year", "billing_month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_attribute_values_attribute_id",
                table: "product_attribute_values",
                column: "attribute_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_attributes_product_id",
                table: "product_attributes",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_content_revisions_category_id",
                table: "product_content_revisions",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_content_revisions_product_id_revision_number",
                table: "product_content_revisions",
                columns: new[] { "product_id", "revision_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_content_revisions_reviewed_by_user_id",
                table: "product_content_revisions",
                column: "reviewed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_content_revisions_submitted_by_user_id",
                table: "product_content_revisions",
                column: "submitted_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ux_product_content_revisions_pending_product",
                table: "product_content_revisions",
                column: "product_id",
                unique: true,
                filter: "\"status\" = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "IX_product_feedback_moderation_logs_admin_user_id_created_at",
                table: "product_feedback_moderation_logs",
                columns: new[] { "admin_user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_product_feedback_moderation_logs_product_feedback_id_create~",
                table: "product_feedback_moderation_logs",
                columns: new[] { "product_feedback_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_product_feedback_replies_product_feedback_id",
                table: "product_feedback_replies",
                column: "product_feedback_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_feedback_replies_replied_by_user_id",
                table: "product_feedback_replies",
                column: "replied_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_feedbacks_order_item_id",
                table: "product_feedbacks",
                column: "order_item_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_feedbacks_user_id_created_at",
                table: "product_feedbacks",
                columns: new[] { "user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_product_feedbacks_visible_product_created_at",
                table: "product_feedbacks",
                columns: new[] { "product_id", "created_at" },
                filter: "\"status\" = 'Visible'");

            migrationBuilder.CreateIndex(
                name: "IX_product_images_product_id",
                table: "product_images",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_variant_values_attribute_value_id",
                table: "product_variant_values",
                column: "attribute_value_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_variants_product_id",
                table: "product_variants",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_Products_category_id",
                table: "Products",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_Products_shop_id",
                table: "Products",
                column: "shop_id");

            migrationBuilder.CreateIndex(
                name: "IX_recovery_requirement_rules_created_by_user_id",
                table: "recovery_requirement_rules",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_replaced_by_token_id",
                table: "refresh_tokens",
                column: "replaced_by_token_id");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_token_hash",
                table: "refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_user_id_expires_at",
                table: "refresh_tokens",
                columns: new[] { "user_id", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "IX_refund_evidence_images_refund_id",
                table: "refund_evidence_images",
                column: "refund_id");

            migrationBuilder.CreateIndex(
                name: "IX_refund_evidence_images_uploaded_by",
                table: "refund_evidence_images",
                column: "uploaded_by");

            migrationBuilder.CreateIndex(
                name: "IX_refunds_initiated_by",
                table: "refunds",
                column: "initiated_by");

            migrationBuilder.CreateIndex(
                name: "IX_refunds_reviewed_by",
                table: "refunds",
                column: "reviewed_by");

            migrationBuilder.CreateIndex(
                name: "ux_refunds_shop_cancellation_per_order",
                table: "refunds",
                column: "order_id",
                unique: true,
                filter: "\"refund_type\" = 'ShopCancellation'");

            migrationBuilder.CreateIndex(
                name: "IX_shop_application_documents_uploaded_by",
                table: "shop_application_documents",
                column: "uploaded_by");

            migrationBuilder.CreateIndex(
                name: "ux_shop_application_current_document",
                table: "shop_application_documents",
                columns: new[] { "application_id", "document_type" },
                unique: true,
                filter: "\"is_current\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_shop_application_review_logs_admin_id",
                table: "shop_application_review_logs",
                column: "admin_id");

            migrationBuilder.CreateIndex(
                name: "IX_shop_application_review_logs_application_id",
                table: "shop_application_review_logs",
                column: "application_id");

            migrationBuilder.CreateIndex(
                name: "IX_shop_applications_applicant_user_id",
                table: "shop_applications",
                column: "applicant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_shop_payment_configs_shop_id",
                table: "shop_payment_configs",
                column: "shop_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_shop_staff_user_id",
                table: "shop_staff",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "shop_staff_unique_0",
                table: "shop_staff",
                columns: new[] { "shop_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_shops_application_id",
                table: "shops",
                column: "application_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_shops_owner_user_id",
                table: "shops",
                column: "owner_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_staff_activity_logs_action_by",
                table: "staff_activity_logs",
                column: "action_by");

            migrationBuilder.CreateIndex(
                name: "IX_staff_activity_logs_order_id",
                table: "staff_activity_logs",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_staff_activity_logs_shop_id",
                table: "staff_activity_logs",
                column: "shop_id");

            migrationBuilder.CreateIndex(
                name: "IX_staff_invitations_invited_by",
                table: "staff_invitations",
                column: "invited_by");

            migrationBuilder.CreateIndex(
                name: "IX_staff_invitations_invited_user_id",
                table: "staff_invitations",
                column: "invited_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_staff_invitations_shop_id",
                table: "staff_invitations",
                column: "shop_id");

            migrationBuilder.CreateIndex(
                name: "IX_trust_level_requirements_trust_level_id",
                table: "trust_level_requirements",
                column: "trust_level_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_trust_levels_name",
                table: "trust_levels",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_trust_levels_rank",
                table: "trust_levels",
                column: "rank",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_trust_score_history_no_show_penalty_id",
                table: "trust_score_history",
                column: "no_show_penalty_id");

            migrationBuilder.CreateIndex(
                name: "IX_trust_score_history_order_id",
                table: "trust_score_history",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_trust_score_history_rule_id",
                table: "trust_score_history",
                column: "rule_id");

            migrationBuilder.CreateIndex(
                name: "IX_trust_score_history_user_id",
                table: "trust_score_history",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_trust_score_rules_created_by",
                table: "trust_score_rules",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ux_default_user_address",
                table: "user_addresses",
                column: "user_id",
                unique: true,
                filter: "\"is_default\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_user_level_progress_user_id",
                table: "user_level_progress",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_role_change_logs_user_id_created_at",
                table: "user_role_change_logs",
                columns: new[] { "user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_user_trust_level_history_new_trust_level_id",
                table: "user_trust_level_history",
                column: "new_trust_level_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_trust_level_history_no_show_penalty_id",
                table: "user_trust_level_history",
                column: "no_show_penalty_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_trust_level_history_order_id",
                table: "user_trust_level_history",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_trust_level_history_previous_trust_level_id",
                table: "user_trust_level_history",
                column: "previous_trust_level_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_trust_level_history_user_id",
                table: "user_trust_level_history",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_trust_scores_trust_level_id",
                table: "user_trust_scores",
                column: "trust_level_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_trust_scores_user_id",
                table: "user_trust_scores",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_phone",
                table: "users",
                column: "phone",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "account_status_logs");

            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "cart_items");

            migrationBuilder.DropTable(
                name: "no_show_penalty_order_history");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "order_status_history");

            migrationBuilder.DropTable(
                name: "payments");

            migrationBuilder.DropTable(
                name: "platform_fee_adjustments");

            migrationBuilder.DropTable(
                name: "product_content_revisions");

            migrationBuilder.DropTable(
                name: "product_feedback_moderation_logs");

            migrationBuilder.DropTable(
                name: "product_feedback_replies");

            migrationBuilder.DropTable(
                name: "product_images");

            migrationBuilder.DropTable(
                name: "product_variant_values");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "refund_evidence_images");

            migrationBuilder.DropTable(
                name: "shop_application_documents");

            migrationBuilder.DropTable(
                name: "shop_application_review_logs");

            migrationBuilder.DropTable(
                name: "shop_payment_configs");

            migrationBuilder.DropTable(
                name: "shop_staff");

            migrationBuilder.DropTable(
                name: "staff_activity_logs");

            migrationBuilder.DropTable(
                name: "staff_invitations");

            migrationBuilder.DropTable(
                name: "trust_level_requirements");

            migrationBuilder.DropTable(
                name: "trust_score_history");

            migrationBuilder.DropTable(
                name: "user_addresses");

            migrationBuilder.DropTable(
                name: "user_level_progress");

            migrationBuilder.DropTable(
                name: "user_role_change_logs");

            migrationBuilder.DropTable(
                name: "user_trust_level_history");

            migrationBuilder.DropTable(
                name: "user_trust_scores");

            migrationBuilder.DropTable(
                name: "order_platform_fees");

            migrationBuilder.DropTable(
                name: "product_feedbacks");

            migrationBuilder.DropTable(
                name: "product_attribute_values");

            migrationBuilder.DropTable(
                name: "refunds");

            migrationBuilder.DropTable(
                name: "trust_score_rules");

            migrationBuilder.DropTable(
                name: "no_show_penalties");

            migrationBuilder.DropTable(
                name: "trust_levels");

            migrationBuilder.DropTable(
                name: "platform_fee_configs");

            migrationBuilder.DropTable(
                name: "platform_fee_statements");

            migrationBuilder.DropTable(
                name: "order_items");

            migrationBuilder.DropTable(
                name: "product_attributes");

            migrationBuilder.DropTable(
                name: "recovery_requirement_rules");

            migrationBuilder.DropTable(
                name: "flash_deal_variants");

            migrationBuilder.DropTable(
                name: "orders");

            migrationBuilder.DropTable(
                name: "flash_deals");

            migrationBuilder.DropTable(
                name: "product_variants");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "categories");

            migrationBuilder.DropTable(
                name: "shops");

            migrationBuilder.DropTable(
                name: "shop_applications");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
