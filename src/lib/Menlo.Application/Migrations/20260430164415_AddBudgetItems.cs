using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace Menlo.Application.Migrations;

/// <inheritdoc />
public partial class AddBudgetItems : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "budget_items",
            schema: "budget_schema",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                budget_id = table.Column<Guid>(type: "uuid", nullable: false),
                category_id = table.Column<Guid>(type: "uuid", nullable: false),
                month = table.Column<int>(type: "integer", nullable: false),
                budget_flow = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                payer_split = table.Column<string>(type: "jsonb", nullable: false),
                attribution_split = table.Column<string>(type: "jsonb", nullable: false),
                adjustment_rule_id = table.Column<Guid>(type: "uuid", nullable: true),
                is_manual_override = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                modified_by = table.Column<Guid>(type: "uuid", nullable: true),
                modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                realized_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                realized_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                spent_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                spent_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                planned_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                planned_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false)
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

        migrationBuilder.CreateIndex(
            name: "ix_budget_items_budget_category_month",
            schema: "budget_schema",
            table: "budget_items",
            columns: new[] { "budget_id", "category_id", "month" },
            unique: true,
            filter: "is_deleted = false");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "budget_items",
            schema: "budget_schema");
    }
}
