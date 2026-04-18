using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace Menlo.Application.Migrations;

/// <inheritdoc />
public partial class AddBudgetSchema : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "budget_schema");

        migrationBuilder.CreateTable(
            name: "budgets",
            schema: "budget_schema",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                household_id = table.Column<Guid>(type: "uuid", nullable: false),
                year = table.Column<int>(type: "integer", nullable: false),
                status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                modified_by = table.Column<Guid>(type: "uuid", nullable: true),
                modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_budgets", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "budget_categories",
            schema: "budget_schema",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                budget_id = table.Column<Guid>(type: "uuid", nullable: false),
                planned_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                planned_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false)
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
            });

        migrationBuilder.CreateIndex(
            name: "ix_budget_categories_budget_id",
            schema: "budget_schema",
            table: "budget_categories",
            column: "budget_id");

        migrationBuilder.CreateIndex(
            name: "ix_budget_categories_parent_id",
            schema: "budget_schema",
            table: "budget_categories",
            column: "parent_id");

        migrationBuilder.CreateIndex(
            name: "ix_budgets_household_id_year",
            schema: "budget_schema",
            table: "budgets",
            columns: new[] { "household_id", "year" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "budget_categories",
            schema: "budget_schema");

        migrationBuilder.DropTable(
            name: "budgets",
            schema: "budget_schema");
    }
}
