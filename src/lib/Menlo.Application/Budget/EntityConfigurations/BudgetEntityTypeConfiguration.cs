using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BudgetAggregate = Menlo.Lib.Budget.Entities.Budget;

namespace Menlo.Application.Budget.EntityConfigurations;

public sealed class BudgetEntityTypeConfiguration : IEntityTypeConfiguration<BudgetAggregate>
{
    /// <summary>The name of the unique index that keeps one budget per year.</summary>
    public const string YearIndexName = "ix_budgets_year";

    public void Configure(EntityTypeBuilder<BudgetAggregate> builder)
    {
        builder.ToTable("budgets", "budget");

        builder.HasKey(budget => budget.Id);

        builder.Property(budget => budget.Id)
            .ValueGeneratedNever();

        builder.Property(budget => budget.Year)
            .IsRequired();

        builder.Property(budget => budget.CreatedBy)
            .IsRequired(false);

        builder.Property(budget => budget.CreatedAt)
            .IsRequired(false);

        builder.Property(budget => budget.ModifiedBy)
            .IsRequired(false);

        builder.Property(budget => budget.ModifiedAt)
            .IsRequired(false);

        builder.HasIndex(budget => budget.Year)
            .IsUnique()
            .HasDatabaseName(YearIndexName);

        // A budget is always loaded with its categories, so its rules can see the whole tree
        builder.HasMany(budget => budget.Categories)
            .WithOne()
            .HasForeignKey(category => category.BudgetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(budget => budget.Categories)
            .AutoInclude();

        builder.Ignore(budget => budget.DomainEvents);
    }
}
