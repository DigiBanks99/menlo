using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Menlo.Application.Migrations;

/// <summary>
/// Maps <c>BudgetItem.RealizedAmount</c> and <c>SpentAmount</c> as optional complex properties.
/// EF scaffolds the amount columns as drop-and-add; they are renamed instead so no data can be lost,
/// and their scale is raised from 2 to 4 per the persistence column type rules.
/// </summary>
public partial class MapBudgetItemRealizedAndSpentAmounts : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "realized_amount",
            schema: "budget_schema",
            table: "budget_items",
            newName: "realized_amount_amount");

        migrationBuilder.RenameColumn(
            name: "realized_currency",
            schema: "budget_schema",
            table: "budget_items",
            newName: "realized_amount_currency");

        migrationBuilder.RenameColumn(
            name: "spent_amount",
            schema: "budget_schema",
            table: "budget_items",
            newName: "spent_amount_amount");

        migrationBuilder.RenameColumn(
            name: "spent_currency",
            schema: "budget_schema",
            table: "budget_items",
            newName: "spent_amount_currency");

        migrationBuilder.AlterColumn<decimal>(
            name: "realized_amount_amount",
            schema: "budget_schema",
            table: "budget_items",
            type: "numeric(18,4)",
            nullable: true,
            oldClrType: typeof(decimal),
            oldType: "numeric(18,2)",
            oldPrecision: 18,
            oldScale: 2,
            oldNullable: true);

        migrationBuilder.AlterColumn<decimal>(
            name: "spent_amount_amount",
            schema: "budget_schema",
            table: "budget_items",
            type: "numeric(18,4)",
            nullable: true,
            oldClrType: typeof(decimal),
            oldType: "numeric(18,2)",
            oldPrecision: 18,
            oldScale: 2,
            oldNullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<decimal>(
            name: "realized_amount_amount",
            schema: "budget_schema",
            table: "budget_items",
            type: "numeric(18,2)",
            precision: 18,
            scale: 2,
            nullable: true,
            oldClrType: typeof(decimal),
            oldType: "numeric(18,4)",
            oldNullable: true);

        migrationBuilder.AlterColumn<decimal>(
            name: "spent_amount_amount",
            schema: "budget_schema",
            table: "budget_items",
            type: "numeric(18,2)",
            precision: 18,
            scale: 2,
            nullable: true,
            oldClrType: typeof(decimal),
            oldType: "numeric(18,4)",
            oldNullable: true);

        migrationBuilder.RenameColumn(
            name: "realized_amount_amount",
            schema: "budget_schema",
            table: "budget_items",
            newName: "realized_amount");

        migrationBuilder.RenameColumn(
            name: "realized_amount_currency",
            schema: "budget_schema",
            table: "budget_items",
            newName: "realized_currency");

        migrationBuilder.RenameColumn(
            name: "spent_amount_amount",
            schema: "budget_schema",
            table: "budget_items",
            newName: "spent_amount");

        migrationBuilder.RenameColumn(
            name: "spent_amount_currency",
            schema: "budget_schema",
            table: "budget_items",
            newName: "spent_currency");
    }
}
