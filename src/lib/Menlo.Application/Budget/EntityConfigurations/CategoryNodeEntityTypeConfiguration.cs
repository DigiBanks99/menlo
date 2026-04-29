using Menlo.Lib.Budget.Entities;
using Menlo.Lib.Budget.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menlo.Application.Budget.EntityConfigurations;

public sealed class CategoryNodeEntityTypeConfiguration : IEntityTypeConfiguration<CategoryNode>
{
    public void Configure(EntityTypeBuilder<CategoryNode> builder)
    {
        builder.ToTable("budget_categories", "budget_schema");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Id)
            .ValueGeneratedNever();

        builder.Property(n => n.Name)
            .HasConversion(
                name => name.Value,
                value => CategoryName.Create(value).Value)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(n => n.ParentId)
            .IsRequired(false);

        builder.Property(n => n.CanonicalCategoryId)
            .IsRequired();

        builder.Property(n => n.BudgetFlow)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(n => n.Attribution)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired(false);

        builder.Property(n => n.Description)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(n => n.IncomeContributor)
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(n => n.ResponsiblePayer)
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Ignore(n => n.PlannedMonthlyAmount);

        // ISoftDeletable
        builder.Property(n => n.IsDeleted)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(n => n.DeletedAt)
            .IsRequired(false);

        builder.Property(n => n.DeletedBy)
            .IsRequired(false);

        // IAuditable
        builder.Property(n => n.CreatedBy)
            .IsRequired(false);

        builder.Property(n => n.CreatedAt)
            .IsRequired(false);

        builder.Property(n => n.ModifiedBy)
            .IsRequired(false);

        builder.Property(n => n.ModifiedAt)
            .IsRequired(false);

        // Relationships
        builder.HasOne<CategoryNode>()
            .WithMany()
            .HasForeignKey(n => n.ParentId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<CanonicalCategory>()
            .WithMany()
            .HasForeignKey(n => n.CanonicalCategoryId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex("budget_id", "ParentId", "Name")
            .IsUnique()
            .HasFilter("is_deleted = false");

        builder.HasIndex("budget_id", "CanonicalCategoryId");
    }
}
