using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace Menlo.Application.Migrations;

/// <inheritdoc />
public partial class RemoveHouseholds : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Household-scoped data is dropped (Requirement: Menlo runs one household, rule 6).
        // Users survive. Deleting first means the drops below never meet a dependent row.
        // Children go before parents; the FKs would cascade, but explicit is clearer.
        migrationBuilder.Sql("DELETE FROM budget_schema.budget_items;");
        migrationBuilder.Sql("DELETE FROM budget_schema.budget_categories;");
        migrationBuilder.Sql("DELETE FROM budget_schema.canonical_categories;");
        migrationBuilder.Sql("DELETE FROM budget_schema.budgets;");
        migrationBuilder.Sql("DELETE FROM shared.onboarding_states;");

        // Dropping the column also drops its FK to households and its index.
        migrationBuilder.DropColumn(
            name: "household_id",
            schema: "shared",
            table: "users");

        migrationBuilder.DropIndex(
            name: "ix_budgets_household_id_year",
            schema: "budget_schema",
            table: "budgets");

        migrationBuilder.DropColumn(
            name: "household_id",
            schema: "budget_schema",
            table: "budgets");

        migrationBuilder.DropTable(
            name: "onboarding_states",
            schema: "shared");

        migrationBuilder.DropTable(
            name: "households",
            schema: "shared");

        migrationBuilder.CreateIndex(
            name: "ix_budgets_year",
            schema: "budget_schema",
            table: "budgets",
            column: "year",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_budgets_year",
            schema: "budget_schema",
            table: "budgets");

        migrationBuilder.AddColumn<Guid>(
            name: "household_id",
            schema: "shared",
            table: "users",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "household_id",
            schema: "budget_schema",
            table: "budgets",
            type: "uuid",
            nullable: false,
            defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

        migrationBuilder.CreateTable(
            name: "households",
            schema: "shared",
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
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_households", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "onboarding_states",
            schema: "shared",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                completed_tasks = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'[]'::jsonb"),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_onboarding_states", x => x.id);
                table.ForeignKey(
                    name: "fk_onboarding_states_users_user_id",
                    column: x => x.user_id,
                    principalSchema: "shared",
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.AddForeignKey(
            name: "fk_users_households_household_id",
            schema: "shared",
            table: "users",
            column: "household_id",
            principalSchema: "shared",
            principalTable: "households",
            principalColumn: "id",
            onDelete: ReferentialAction.SetNull);

        migrationBuilder.CreateIndex(
            name: "ix_budgets_household_id_year",
            schema: "budget_schema",
            table: "budgets",
            columns: new[] { "household_id", "year" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_onboarding_states_user_id",
            schema: "shared",
            table: "onboarding_states",
            column: "user_id",
            unique: true);
    }
}
