using Menlo.Utilities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menlo.Utilities.Configurations;

public partial class ApplianceUsageConfiguration : IEntityTypeConfiguration<ApplianceUsage>
{
    public void Configure(EntityTypeBuilder<ApplianceUsage> entity)
    {
        entity.HasKey(e => new { e.ApplianceId, e.ElectricityUsageId });

        entity.ToTable("ApplianceUsages", "utilities");

        entity.Property(e => e.HoursOfUse).HasColumnType("decimal(18, 2)");

        entity.HasOne(d => d.Appliance).WithMany(p => p.ApplianceUsages)
            .HasForeignKey(d => d.ApplianceId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        entity.HasOne(d => d.ElectricityUsage).WithMany(p => p.ApplianceUsages)
            .HasForeignKey(d => d.ElectricityUsageId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        OnConfigurePartial(entity);
    }

    partial void OnConfigurePartial(EntityTypeBuilder<ApplianceUsage> entity);
}
