using Menlo.Lib.Budget.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menlo.Application.Budget.EntityConfigurations;

public sealed class CanonicalCategoryEntityTypeConfiguration : IEntityTypeConfiguration<CanonicalCategory>
{
    public void Configure(EntityTypeBuilder<CanonicalCategory> builder)
    {
        builder.ToTable("canonical_categories", "budget_schema");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .ValueGeneratedNever();

        builder.Property(c => c.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.CreatedBy)
            .IsRequired(false);

        builder.Property(c => c.CreatedAt)
            .IsRequired(false);

        builder.Property(c => c.ModifiedBy)
            .IsRequired(false);

        builder.Property(c => c.ModifiedAt)
            .IsRequired(false);
    }
}
