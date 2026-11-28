using Menlo.Lib.Budget.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menlo.Application.Budget.EntityConfigurations;

public sealed class CategoryEntityTypeConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories", "budget");

        builder.HasKey(category => category.Id);

        builder.Property(category => category.Id)
            .ValueGeneratedNever();

        builder.Property(category => category.BudgetId)
            .IsRequired();

        builder.Property(category => category.Name)
            .IsRequired()
            .HasMaxLength(Category.MaxNameLength);

        builder.Property(category => category.ParentId)
            .IsRequired(false);

        // A subcategory sits under a top-level category of the same budget; the budget keeps that rule
        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(category => category.ParentId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Property(category => category.CreatedBy)
            .IsRequired(false);

        builder.Property(category => category.CreatedAt)
            .IsRequired(false);

        builder.Property(category => category.ModifiedBy)
            .IsRequired(false);

        builder.Property(category => category.ModifiedAt)
            .IsRequired(false);

        builder.Ignore(category => category.IsTopLevel);
    }
}
