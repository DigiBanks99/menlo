using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace Menlo.Application.Migrations;

/// <inheritdoc />
public partial class AddReceipts : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "financial");

        migrationBuilder.CreateTable(
            name: "receipts",
            schema: "financial",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                year = table.Column<int>(type: "integer", nullable: false),
                taken_by = table.Column<Guid>(type: "uuid", nullable: false),
                taken_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                budget_view = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                reading = table.Column<string>(type: "jsonb", nullable: true),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                modified_by = table.Column<Guid>(type: "uuid", nullable: true),
                modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_receipts", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "receipt_photos",
            schema: "financial",
            columns: table => new
            {
                receipt_id = table.Column<Guid>(type: "uuid", nullable: false),
                position = table.Column<int>(type: "integer", nullable: false),
                jpeg = table.Column<byte[]>(type: "bytea", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_receipt_photos", x => new { x.receipt_id, x.position });
                table.ForeignKey(
                    name: "fk_receipt_photos_receipts_receipt_id",
                    column: x => x.receipt_id,
                    principalSchema: "financial",
                    principalTable: "receipts",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_receipts_status_taken_at",
            schema: "financial",
            table: "receipts",
            columns: new[] { "status", "taken_at" });

        migrationBuilder.CreateIndex(
            name: "ix_receipts_year_taken_at",
            schema: "financial",
            table: "receipts",
            columns: new[] { "year", "taken_at" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "receipt_photos",
            schema: "financial");

        migrationBuilder.DropTable(
            name: "receipts",
            schema: "financial");
    }
}
