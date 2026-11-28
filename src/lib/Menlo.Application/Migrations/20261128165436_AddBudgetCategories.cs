using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace Menlo.Application.Migrations;

/// <inheritdoc />
public partial class AddBudgetCategories : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "categories",
            schema: "budget",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                budget_id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                modified_by = table.Column<Guid>(type: "uuid", nullable: true),
                modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_categories", x => x.id);
                table.ForeignKey(
                    name: "fk_categories_budgets_budget_id",
                    column: x => x.budget_id,
                    principalSchema: "budget",
                    principalTable: "budgets",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_categories_categories_parent_id",
                    column: x => x.parent_id,
                    principalSchema: "budget",
                    principalTable: "categories",
                    principalColumn: "id");
            });

        migrationBuilder.CreateIndex(
            name: "ix_categories_budget_id",
            schema: "budget",
            table: "categories",
            column: "budget_id");

        migrationBuilder.CreateIndex(
            name: "ix_categories_parent_id",
            schema: "budget",
            table: "categories",
            column: "parent_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "categories",
            schema: "budget");
    }
}
