using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace Menlo.Application.Migrations;

/// <inheritdoc />
public partial class AddHouseholdIdentityFoundation : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<bool>(
            name: "is_deleted",
            schema: "shared",
            table: "users",
            type: "boolean",
            nullable: false,
            defaultValue: false,
            oldClrType: typeof(bool),
            oldType: "boolean");

        migrationBuilder.AddColumn<Guid>(
            name: "household_id",
            schema: "shared",
            table: "users",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "households",
            schema: "shared",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
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
                table.PrimaryKey("pk_households", x => x.id);
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

        // Seed one default household
        Guid defaultHouseholdId = new("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
        migrationBuilder.InsertData(
            schema: "shared",
            table: "households",
            columns: ["id", "name", "is_deleted"],
            values: new object[] { defaultHouseholdId, "Default Household", false });

        // Assign all existing users to the default household
        migrationBuilder.Sql(
            $"UPDATE shared.users SET household_id = '{defaultHouseholdId}'");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "fk_users_households_household_id",
            schema: "shared",
            table: "users");

        migrationBuilder.DropTable(
            name: "households",
            schema: "shared");

        migrationBuilder.DropColumn(
            name: "household_id",
            schema: "shared",
            table: "users");

        migrationBuilder.AlterColumn<bool>(
            name: "is_deleted",
            schema: "shared",
            table: "users",
            type: "boolean",
            nullable: false,
            oldClrType: typeof(bool),
            oldType: "boolean",
            oldDefaultValue: false);
    }
}
