using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace Menlo.Application.Migrations;

/// <inheritdoc />
public partial class EnrichCategoryNodesAndAddCanonicalCategories : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_budget_categories_budget_id",
            schema: "budget_schema",
            table: "budget_categories");

        migrationBuilder.DropColumn(
            name: "planned_amount",
            schema: "budget_schema",
            table: "budget_categories");

        migrationBuilder.DropColumn(
            name: "planned_currency",
            schema: "budget_schema",
            table: "budget_categories");

        migrationBuilder.AddColumn<string>(
            name: "attribution",
            schema: "budget_schema",
            table: "budget_categories",
            type: "character varying(20)",
            maxLength: 20,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "budget_flow",
            schema: "budget_schema",
            table: "budget_categories",
            type: "character varying(20)",
            maxLength: 20,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<Guid>(
            name: "canonical_category_id",
            schema: "budget_schema",
            table: "budget_categories",
            type: "uuid",
            nullable: false,
            defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "created_at",
            schema: "budget_schema",
            table: "budget_categories",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "created_by",
            schema: "budget_schema",
            table: "budget_categories",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "deleted_at",
            schema: "budget_schema",
            table: "budget_categories",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "deleted_by",
            schema: "budget_schema",
            table: "budget_categories",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "description",
            schema: "budget_schema",
            table: "budget_categories",
            type: "character varying(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "income_contributor",
            schema: "budget_schema",
            table: "budget_categories",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "is_deleted",
            schema: "budget_schema",
            table: "budget_categories",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "modified_at",
            schema: "budget_schema",
            table: "budget_categories",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "modified_by",
            schema: "budget_schema",
            table: "budget_categories",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "responsible_payer",
            schema: "budget_schema",
            table: "budget_categories",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);

        migrationBuilder.CreateTable(
            name: "canonical_categories",
            schema: "budget_schema",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                modified_by = table.Column<Guid>(type: "uuid", nullable: true),
                modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_canonical_categories", x => x.id);
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

        migrationBuilder.AddForeignKey(
            name: "fk_budget_categories_canonical_categories_canonical_category_id",
            schema: "budget_schema",
            table: "budget_categories",
            column: "canonical_category_id",
            principalSchema: "budget_schema",
            principalTable: "canonical_categories",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "fk_budget_categories_canonical_categories_canonical_category_id",
            schema: "budget_schema",
            table: "budget_categories");

        migrationBuilder.DropTable(
            name: "canonical_categories",
            schema: "budget_schema");

        migrationBuilder.DropIndex(
            name: "ix_budget_categories_budget_id_canonical_category_id",
            schema: "budget_schema",
            table: "budget_categories");

        migrationBuilder.DropIndex(
            name: "ix_budget_categories_budget_id_parent_id_name",
            schema: "budget_schema",
            table: "budget_categories");

        migrationBuilder.DropIndex(
            name: "ix_budget_categories_canonical_category_id",
            schema: "budget_schema",
            table: "budget_categories");

        migrationBuilder.DropColumn(
            name: "attribution",
            schema: "budget_schema",
            table: "budget_categories");

        migrationBuilder.DropColumn(
            name: "budget_flow",
            schema: "budget_schema",
            table: "budget_categories");

        migrationBuilder.DropColumn(
            name: "canonical_category_id",
            schema: "budget_schema",
            table: "budget_categories");

        migrationBuilder.DropColumn(
            name: "created_at",
            schema: "budget_schema",
            table: "budget_categories");

        migrationBuilder.DropColumn(
            name: "created_by",
            schema: "budget_schema",
            table: "budget_categories");

        migrationBuilder.DropColumn(
            name: "deleted_at",
            schema: "budget_schema",
            table: "budget_categories");

        migrationBuilder.DropColumn(
            name: "deleted_by",
            schema: "budget_schema",
            table: "budget_categories");

        migrationBuilder.DropColumn(
            name: "description",
            schema: "budget_schema",
            table: "budget_categories");

        migrationBuilder.DropColumn(
            name: "income_contributor",
            schema: "budget_schema",
            table: "budget_categories");

        migrationBuilder.DropColumn(
            name: "is_deleted",
            schema: "budget_schema",
            table: "budget_categories");

        migrationBuilder.DropColumn(
            name: "modified_at",
            schema: "budget_schema",
            table: "budget_categories");

        migrationBuilder.DropColumn(
            name: "modified_by",
            schema: "budget_schema",
            table: "budget_categories");

        migrationBuilder.DropColumn(
            name: "responsible_payer",
            schema: "budget_schema",
            table: "budget_categories");

        migrationBuilder.AddColumn<decimal>(
            name: "planned_amount",
            schema: "budget_schema",
            table: "budget_categories",
            type: "numeric(18,2)",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<string>(
            name: "planned_currency",
            schema: "budget_schema",
            table: "budget_categories",
            type: "character varying(3)",
            maxLength: 3,
            nullable: false,
            defaultValue: "");

        migrationBuilder.CreateIndex(
            name: "ix_budget_categories_budget_id",
            schema: "budget_schema",
            table: "budget_categories",
            column: "budget_id");
    }
}
