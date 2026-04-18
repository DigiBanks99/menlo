using Menlo.Lib.Budget.Entities;
using Menlo.Lib.Budget.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BudgetAggregate = Menlo.Lib.Budget.Entities.Budget;

namespace Menlo.Application.Budget.EntityConfigurations;

public sealed class BudgetEntityTypeConfiguration : IEntityTypeConfiguration<BudgetAggregate>
{
    public void Configure(EntityTypeBuilder<BudgetAggregate> builder)
    {
        builder.ToTable("budgets", "budget_schema");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Id)
            .ValueGeneratedNever();

        builder.Property(b => b.HouseholdId)
            .IsRequired();

        builder.Property(b => b.Year)
            .IsRequired();

        builder.Property(b => b.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(b => b.CreatedBy)
            .IsRequired(false);

        builder.Property(b => b.CreatedAt)
            .IsRequired(false);

        builder.Property(b => b.ModifiedBy)
            .IsRequired(false);

        builder.Property(b => b.ModifiedAt)
            .IsRequired(false);

        builder.Property(b => b.IsDeleted)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(b => b.DeletedAt)
            .IsRequired(false);

        builder.Property(b => b.DeletedBy)
            .IsRequired(false);

        builder.HasMany(b => b.Categories)
            .WithOne()
            .HasForeignKey("budget_id")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(b => b.Categories)
            .HasField("_categories")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(b => new { b.HouseholdId, b.Year })
            .IsUnique()
            .HasDatabaseName("ix_budgets_household_id_year");

        builder.Ignore(b => b.DomainEvents);
        builder.Ignore(b => b.TotalPlannedMonthlyAmount);
    }
}
