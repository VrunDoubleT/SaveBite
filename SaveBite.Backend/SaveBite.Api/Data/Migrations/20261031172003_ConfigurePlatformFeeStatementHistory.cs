using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaveBite.Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class ConfigurePlatformFeeStatementHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlatformFeeStatementHistories_platform_fee_statements_State~",
                table: "PlatformFeeStatementHistories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PlatformFeeStatementHistories",
                table: "PlatformFeeStatementHistories");

            migrationBuilder.RenameTable(
                name: "PlatformFeeStatementHistories",
                newName: "platform_fee_statement_histories");

            migrationBuilder.RenameColumn(
                name: "Reason",
                table: "platform_fee_statement_histories",
                newName: "reason");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "platform_fee_statement_histories",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "StatementId",
                table: "platform_fee_statement_histories",
                newName: "statement_id");

            migrationBuilder.RenameColumn(
                name: "RelatedRefundId",
                table: "platform_fee_statement_histories",
                newName: "related_refund_id");

            migrationBuilder.RenameColumn(
                name: "RelatedOrderId",
                table: "platform_fee_statement_histories",
                newName: "related_order_id");

            migrationBuilder.RenameColumn(
                name: "PreviousAmount",
                table: "platform_fee_statement_histories",
                newName: "previous_amount");

            migrationBuilder.RenameColumn(
                name: "NewAmount",
                table: "platform_fee_statement_histories",
                newName: "new_amount");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "platform_fee_statement_histories",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "ChangeType",
                table: "platform_fee_statement_histories",
                newName: "change_type");

            migrationBuilder.RenameColumn(
                name: "ChangeAmount",
                table: "platform_fee_statement_histories",
                newName: "change_amount");

            migrationBuilder.RenameIndex(
                name: "IX_PlatformFeeStatementHistories_StatementId",
                table: "platform_fee_statement_histories",
                newName: "IX_platform_fee_statement_histories_statement_id");

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "platform_fee_statement_histories",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<decimal>(
                name: "previous_amount",
                table: "platform_fee_statement_histories",
                type: "numeric(14,2)",
                precision: 14,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "new_amount",
                table: "platform_fee_statement_histories",
                type: "numeric(14,2)",
                precision: 14,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<string>(
                name: "change_type",
                table: "platform_fee_statement_histories",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<decimal>(
                name: "change_amount",
                table: "platform_fee_statement_histories",
                type: "numeric(14,2)",
                precision: 14,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddPrimaryKey(
                name: "PK_platform_fee_statement_histories",
                table: "platform_fee_statement_histories",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_platform_fee_statement_histories_platform_fee_statements_st~",
                table: "platform_fee_statement_histories",
                column: "statement_id",
                principalTable: "platform_fee_statements",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_platform_fee_statement_histories_platform_fee_statements_st~",
                table: "platform_fee_statement_histories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_platform_fee_statement_histories",
                table: "platform_fee_statement_histories");

            migrationBuilder.RenameTable(
                name: "platform_fee_statement_histories",
                newName: "PlatformFeeStatementHistories");

            migrationBuilder.RenameColumn(
                name: "reason",
                table: "PlatformFeeStatementHistories",
                newName: "Reason");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "PlatformFeeStatementHistories",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "statement_id",
                table: "PlatformFeeStatementHistories",
                newName: "StatementId");

            migrationBuilder.RenameColumn(
                name: "related_refund_id",
                table: "PlatformFeeStatementHistories",
                newName: "RelatedRefundId");

            migrationBuilder.RenameColumn(
                name: "related_order_id",
                table: "PlatformFeeStatementHistories",
                newName: "RelatedOrderId");

            migrationBuilder.RenameColumn(
                name: "previous_amount",
                table: "PlatformFeeStatementHistories",
                newName: "PreviousAmount");

            migrationBuilder.RenameColumn(
                name: "new_amount",
                table: "PlatformFeeStatementHistories",
                newName: "NewAmount");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "PlatformFeeStatementHistories",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "change_type",
                table: "PlatformFeeStatementHistories",
                newName: "ChangeType");

            migrationBuilder.RenameColumn(
                name: "change_amount",
                table: "PlatformFeeStatementHistories",
                newName: "ChangeAmount");

            migrationBuilder.RenameIndex(
                name: "IX_platform_fee_statement_histories_statement_id",
                table: "PlatformFeeStatementHistories",
                newName: "IX_PlatformFeeStatementHistories_StatementId");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "PlatformFeeStatementHistories",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()");

            migrationBuilder.AlterColumn<decimal>(
                name: "PreviousAmount",
                table: "PlatformFeeStatementHistories",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(14,2)",
                oldPrecision: 14,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "NewAmount",
                table: "PlatformFeeStatementHistories",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(14,2)",
                oldPrecision: 14,
                oldScale: 2);

            migrationBuilder.AlterColumn<int>(
                name: "ChangeType",
                table: "PlatformFeeStatementHistories",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<decimal>(
                name: "ChangeAmount",
                table: "PlatformFeeStatementHistories",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(14,2)",
                oldPrecision: 14,
                oldScale: 2);

            migrationBuilder.AddPrimaryKey(
                name: "PK_PlatformFeeStatementHistories",
                table: "PlatformFeeStatementHistories",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PlatformFeeStatementHistories_platform_fee_statements_State~",
                table: "PlatformFeeStatementHistories",
                column: "StatementId",
                principalTable: "platform_fee_statements",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
