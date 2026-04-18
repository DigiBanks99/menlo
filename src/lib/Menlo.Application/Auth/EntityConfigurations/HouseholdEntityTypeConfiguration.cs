using Menlo.Lib.Auth.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menlo.Application.Auth.EntityConfigurations;

public sealed class HouseholdEntityTypeConfiguration : IEntityTypeConfiguration<Household>
{
    public void Configure(EntityTypeBuilder<Household> builder)
    {
        builder.ToTable("households", "shared");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.Id)
            .ValueGeneratedNever();

        builder.Property(h => h.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(h => h.CreatedBy)
            .IsRequired(false);

        builder.Property(h => h.CreatedAt)
            .IsRequired(false);

        builder.Property(h => h.ModifiedBy)
            .IsRequired(false);

        builder.Property(h => h.ModifiedAt)
            .IsRequired(false);

        builder.Property(h => h.IsDeleted)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(h => h.DeletedAt)
            .IsRequired(false);

        builder.Property(h => h.DeletedBy)
            .IsRequired(false);

        builder.Ignore(h => h.DomainEvents);
    }
}
