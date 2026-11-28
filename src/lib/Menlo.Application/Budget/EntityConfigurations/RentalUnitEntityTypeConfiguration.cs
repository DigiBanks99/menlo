using Menlo.Lib.Budget.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menlo.Application.Budget.EntityConfigurations;

public sealed class RentalUnitEntityTypeConfiguration : IEntityTypeConfiguration<RentalUnit>
{
    public void Configure(EntityTypeBuilder<RentalUnit> builder)
    {
        builder.ToTable("rental_units", "budget");

        builder.HasKey(unit => unit.Id);

        builder.Property(unit => unit.Id)
            .ValueGeneratedNever();

        builder.Property(unit => unit.Name)
            .HasMaxLength(RentalUnit.MaxNameLength)
            .IsRequired();

        builder.Property(unit => unit.CreatedBy)
            .IsRequired(false);

        builder.Property(unit => unit.CreatedAt)
            .IsRequired(false);

        builder.Property(unit => unit.ModifiedBy)
            .IsRequired(false);

        builder.Property(unit => unit.ModifiedAt)
            .IsRequired(false);

        builder.Ignore(unit => unit.DomainEvents);
    }
}
