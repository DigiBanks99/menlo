using Menlo.Api.Persistence.Converters;
using Menlo.Lib.Budget.Entities;
using Menlo.Lib.Budget.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menlo.Api.Persistence.Configurations;

/// <summary>
/// EF Core entity configuration for the Budget aggregate root.
/// Maps to the budget.budgets table in PostgreSQL.
/// </summary>
public sealed class BudgetConfiguration : IEntityTypeConfiguration<Budget>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Budget> builder)
    {
        // Table mapping with schema
        builder.ToTable("budgets", "budget");

        // Primary key
        builder.HasKey(b => b.Id);

        // Property configurations
        builder.Property(b => b.Id)
            .HasConversion<BudgetIdConverter>()
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(b => b.OwnerId)
            .HasConversion<UserIdConverter>()
            .HasColumnName("owner_id")
            .IsRequired();

        builder.Property(b => b.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        // Budget Period as complex type (year and month in separate columns)
        builder.ComplexProperty(b => b.Period, period =>
        {
            period.Property(p => p.Year)
                .HasColumnName("period_year")
                .IsRequired();

            period.Property(p => p.Month)
                .HasColumnName("period_month")
                .IsRequired();
        });

        builder.Property(b => b.Currency)
            .HasColumnName("currency")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(b => b.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // Audit columns
        builder.Property(b => b.CreatedBy)
            .HasConversion<NullableUserIdConverter>()
            .HasColumnName("created_by");

        builder.Property(b => b.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(b => b.ModifiedBy)
            .HasConversion<NullableUserIdConverter>()
            .HasColumnName("modified_by");

        builder.Property(b => b.ModifiedAt)
            .HasColumnName("modified_at");

        // Relationships - Budget has many root categories
        builder.HasMany(b => b.Categories)
            .WithOne()
            .HasForeignKey(c => c.BudgetId)
            .OnDelete(DeleteBehavior.Cascade);

        // Access the backing field for Categories collection
        builder.Navigation(b => b.Categories)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Indexes
        builder.HasIndex(b => b.OwnerId)
            .HasDatabaseName("ix_budgets_owner_id");

        builder.HasIndex(b => new { b.OwnerId, b.Name })
            .HasDatabaseName("ix_budgets_owner_name");

        // Ignore domain events (not persisted)
        builder.Ignore(b => b.DomainEvents);
    }
}
