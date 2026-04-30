using Menlo.Lib.Budget.Entities;
using Menlo.Lib.Budget.Enums;
using Menlo.Lib.Budget.ValueObjects;
using Menlo.Lib.Common.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace Menlo.Application.Budget.EntityConfigurations;

public sealed class BudgetItemEntityTypeConfiguration : IEntityTypeConfiguration<BudgetItem>
{
    public void Configure(EntityTypeBuilder<BudgetItem> builder)
    {
        builder.ToTable("budget_items", "budget_schema");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id)
            .ValueGeneratedNever();

        builder.Property(i => i.BudgetId)
            .IsRequired();

        builder.Property(i => i.CategoryId)
            .IsRequired();

        builder.Property(i => i.Month)
            .IsRequired();

        builder.Property(i => i.BudgetFlow)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // Money value object: PlannedAmount (required)
        builder.ComplexProperty(i => i.PlannedAmount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("planned_amount")
                .HasPrecision(18, 2)
                .IsRequired();
            money.Property(m => m.Currency)
                .HasColumnName("planned_currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        // Nullable Money? properties cannot use ComplexProperty in EF Core 8.
        // Store as shadow properties for schema correctness; entity manages them internally.
        builder.Ignore(i => i.RealizedAmount);
        builder.Ignore(i => i.SpentAmount);

        builder.Property<decimal?>("realized_amount")
            .HasPrecision(18, 2);

        builder.Property<string?>("realized_currency")
            .HasMaxLength(3);

        builder.Property<decimal?>("spent_amount")
            .HasPrecision(18, 2);

        builder.Property<string?>("spent_currency")
            .HasMaxLength(3);

        // PayerSplit as JSONB
        builder.Property(i => i.PayerSplit)
            .HasColumnType("jsonb")
            .HasConversion(
                split => JsonSerializer.Serialize(
                    split.Allocations.Select(a => new PayerAllocationDto(a.UserId.Value, a.Percent)).ToList(),
                    JsonSerializerOptions.Default),
                json => DeserializePayerSplit(json))
            .IsRequired();

        // AttributionSplit as JSONB
        builder.Property(i => i.AttributionSplit)
            .HasColumnType("jsonb")
            .HasConversion(
                split => JsonSerializer.Serialize(
                    split.Allocations.Select(a => new AttributionAllocationDto(a.Attribution.ToString(), a.Percent)).ToList(),
                    JsonSerializerOptions.Default),
                json => DeserializeAttributionSplit(json))
            .IsRequired();

        builder.Property(i => i.AdjustmentRuleId)
            .IsRequired(false);

        builder.Property(i => i.IsManualOverride)
            .HasDefaultValue(true)
            .IsRequired();

        // ISoftDeletable
        builder.Property(i => i.IsDeleted)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(i => i.DeletedAt)
            .IsRequired(false);

        builder.Property(i => i.DeletedBy)
            .IsRequired(false);

        // IAuditable
        builder.Property(i => i.CreatedBy)
            .IsRequired(false);

        builder.Property(i => i.CreatedAt)
            .IsRequired(false);

        builder.Property(i => i.ModifiedBy)
            .IsRequired(false);

        builder.Property(i => i.ModifiedAt)
            .IsRequired(false);

        // Unique index: one item per category per month (active only)
        builder.HasIndex(i => new { i.BudgetId, i.CategoryId, i.Month })
            .IsUnique()
            .HasFilter("is_deleted = false")
            .HasDatabaseName("ix_budget_items_budget_category_month");
    }

    private static PayerSplit DeserializePayerSplit(string json)
    {
        List<PayerAllocationDto>? dtos = JsonSerializer.Deserialize<List<PayerAllocationDto>>(json);
        if (dtos is null || dtos.Count == 0)
        {
            return PayerSplit.Create([new PayerAllocation(new UserId(Guid.Empty), 100)]).Value;
        }

        List<PayerAllocation> allocations = dtos
            .Select(d => new PayerAllocation(new UserId(d.UserId), d.Percent))
            .ToList();

        return PayerSplit.Create(allocations).Value;
    }

    private static AttributionSplit DeserializeAttributionSplit(string json)
    {
        List<AttributionAllocationDto>? dtos = JsonSerializer.Deserialize<List<AttributionAllocationDto>>(json);
        if (dtos is null || dtos.Count == 0)
        {
            return AttributionSplit.Create([new AttributionAllocation(Attribution.Main, 100)]).Value;
        }

        List<AttributionAllocation> allocations = dtos
            .Select(d => new AttributionAllocation(Enum.Parse<Attribution>(d.Attribution), d.Percent))
            .ToList();

        return AttributionSplit.Create(allocations).Value;
    }

    private sealed record PayerAllocationDto(Guid UserId, int Percent);
    private sealed record AttributionAllocationDto(string Attribution, int Percent);
}
