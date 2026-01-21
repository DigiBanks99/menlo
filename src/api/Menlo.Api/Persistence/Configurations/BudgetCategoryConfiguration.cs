using Menlo.Api.Persistence.Converters;
using Menlo.Lib.Budget.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menlo.Api.Persistence.Configurations;

/// <summary>
/// EF Core entity configuration for the BudgetCategory entity.
/// Maps to the budget.budget_categories table in PostgreSQL.
/// Uses the OwnsOne pattern for the Money value object per spec DT-003.
/// </summary>
public sealed class BudgetCategoryConfiguration : IEntityTypeConfiguration<BudgetCategory>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<BudgetCategory> builder)
    {
        // Table mapping with schema
        builder.ToTable("budget_categories", "budget");

        // Primary key
        builder.HasKey(c => c.Id);

        // Property configurations
        builder.Property(c => c.Id)
            .HasConversion<BudgetCategoryIdConverter>()
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(c => c.BudgetId)
            .HasConversion<BudgetIdConverter>()
            .HasColumnName("budget_id")
            .IsRequired();

        builder.Property(c => c.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.Description)
            .HasColumnName("description")
            .HasMaxLength(500);

        builder.Property(c => c.ParentId)
            .HasConversion<NullableBudgetCategoryIdConverter>()
            .HasColumnName("parent_id");

        builder.Property(c => c.DisplayOrder)
            .HasColumnName("display_order")
            .IsRequired();

        // Money value object - use Ignore and shadow properties since Money is a nullable struct
        // PlannedAmount is nullable Money?, we'll store as two separate columns
        // EF Core will use the shadow properties for persistence
        builder.Ignore(c => c.PlannedAmount);

        // Add shadow properties for the Money value object columns
        builder.Property<decimal?>("PlannedAmountValue")
            .HasColumnName("planned_amount")
            .HasPrecision(19, 4);

        builder.Property<string?>("PlannedAmountCurrency")
            .HasColumnName("planned_currency")
            .HasMaxLength(3);

        // Self-referencing relationship for hierarchy (parent-child)
        builder.HasOne<BudgetCategory>()
            .WithMany(c => c.Children)
            .HasForeignKey(c => c.ParentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Access the backing field for Children collection
        builder.Navigation(c => c.Children)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Indexes
        builder.HasIndex(c => c.BudgetId)
            .HasDatabaseName("ix_budget_categories_budget_id");

        builder.HasIndex(c => c.ParentId)
            .HasDatabaseName("ix_budget_categories_parent_id");

        builder.HasIndex(c => new { c.BudgetId, c.ParentId, c.Name })
            .HasDatabaseName("ix_budget_categories_budget_parent_name")
            .IsUnique();
    }
}
