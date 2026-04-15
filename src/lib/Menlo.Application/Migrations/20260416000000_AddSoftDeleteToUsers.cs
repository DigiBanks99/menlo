using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace Menlo.Application.Migrations;

/// <inheritdoc />
public partial class AddSoftDeleteToUsers : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "is_deleted",
            schema: "shared",
            table: "users",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "deleted_at",
            schema: "shared",
            table: "users",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "deleted_by",
            schema: "shared",
            table: "users",
            type: "uuid",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "is_deleted",
            schema: "shared",
            table: "users");

        migrationBuilder.DropColumn(
            name: "deleted_at",
            schema: "shared",
            table: "users");

        migrationBuilder.DropColumn(
            name: "deleted_by",
            schema: "shared",
            table: "users");
    }
}
