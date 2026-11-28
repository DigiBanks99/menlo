using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace Menlo.Application.Migrations;

/// <inheritdoc />
public partial class RemoveShippedBudget : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "budget_categories",
            schema: "budget_schema");

        migrationBuilder.DropTable(
            name: "budget_items",
            schema: "budget_schema");

        migrationBuilder.DropTable(
            name: "canonical_categories",
            schema: "budget_schema");

        migrationBuilder.DropTable(
            name: "budgets",
            schema: "budget_schema");

        // The shipped budget's data is test data and is dropped with it; the rebuilt budget creates its own tables
        migrationBuilder.Sql("DROP SCHEMA IF EXISTS budget_schema;");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "budget_schema");

        migrationBuilder.CreateTable(
            name: "budgets",
            schema: "budget_schema",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                modified_by = table.Column<Guid>(type: "uuid", nullable: true),
                status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                year = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_budgets", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "canonical_categories",
            schema: "budget_schema",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                modified_by = table.Column<Guid>(type: "uuid", nullable: true),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_canonical_categories", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "budget_items",
            schema: "budget_schema",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                adjustment_rule_id = table.Column<Guid>(type: "uuid", nullable: true),
                attribution_split = table.Column<string>(type: "jsonb", nullable: false),
                budget_flow = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                budget_id = table.Column<Guid>(type: "uuid", nullable: false),
                category_id = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                is_manual_override = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                modified_by = table.Column<Guid>(type: "uuid", nullable: true),
                month = table.Column<int>(type: "integer", nullable: false),
                payer_split = table.Column<string>(type: "jsonb", nullable: false),
                planned_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                planned_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                realized_amount_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                realized_amount_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                spent_amount_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                spent_amount_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_budget_items", x => x.id);
                table.ForeignKey(
                    name: "fk_budget_items_budgets_budget_id",
                    column: x => x.budget_id,
                    principalSchema: "budget_schema",
                    principalTable: "budgets",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "budget_categories",
            schema: "budget_schema",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                attribution = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                budget_flow = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                canonical_category_id = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                income_contributor = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                modified_by = table.Column<Guid>(type: "uuid", nullable: true),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                responsible_payer = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                budget_id = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_budget_categories", x => x.id);
                table.ForeignKey(
                    name: "fk_budget_categories_budget_categories_parent_id",
                    column: x => x.parent_id,
                    principalSchema: "budget_schema",
                    principalTable: "budget_categories",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_budget_categories_budgets_budget_id",
                    column: x => x.budget_id,
                    principalSchema: "budget_schema",
                    principalTable: "budgets",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_budget_categories_canonical_categories_canonical_category_id",
                    column: x => x.canonical_category_id,
                    principalSchema: "budget_schema",
                    principalTable: "canonical_categories",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "ix_budget_categories_budget_id_canonical_category_id",
            schema: "budget_schema",
            table: "budget_categories",
            columns: new[] { "budget_id", "canonical_category_id" });

        migrationBuilder.CreateIndex(
            name: "ix_budget_categories_budget_id_parent_id_name",
            schema: "budget_schema",
            table: "budget_categories",
            columns: new[] { "budget_id", "parent_id", "name" },
            unique: true,
            filter: "is_deleted = false");

        migrationBuilder.CreateIndex(
            name: "ix_budget_categories_canonical_category_id",
            schema: "budget_schema",
            table: "budget_categories",
            column: "canonical_category_id");

        migrationBuilder.CreateIndex(
            name: "ix_budget_categories_parent_id",
            schema: "budget_schema",
            table: "budget_categories",
            column: "parent_id");

        migrationBuilder.CreateIndex(
            name: "ix_budget_items_budget_category_month",
            schema: "budget_schema",
            table: "budget_items",
            columns: new[] { "budget_id", "category_id", "month" },
            unique: true,
            filter: "is_deleted = false");

        migrationBuilder.CreateIndex(
            name: "ix_budgets_year",
            schema: "budget_schema",
            table: "budgets",
            column: "year",
            unique: true);
    }
}
