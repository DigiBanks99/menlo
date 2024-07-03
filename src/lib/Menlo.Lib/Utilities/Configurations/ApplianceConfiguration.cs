using Menlo.Lib.Utilities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menlo.Lib.Utilities.Configurations;

public partial class ApplianceConfiguration : IEntityTypeConfiguration<Appliance>
{
    public void Configure(EntityTypeBuilder<Appliance> entity)
    {
        entity.HasKey(e => e.Id).HasName("PK_Appliances_Id");

        entity.ToTable("Appliances", "utilities");

        entity.Property(e => e.Id).ValueGeneratedNever();
        entity.Property(e => e.Description).HasMaxLength(2000);
        entity.Property(e => e.Name)
            .HasMaxLength(255)
            .IsUnicode(false);
        entity.Property(e => e.PurchaseDate).HasColumnType("datetime");

        OnConfigurePartial(entity);
    }

    partial void OnConfigurePartial(EntityTypeBuilder<Appliance> entity);
}
