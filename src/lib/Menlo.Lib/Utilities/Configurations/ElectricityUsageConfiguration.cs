using Menlo.Lib.Utilities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menlo.Lib.Utilities.Configurations;

public partial class ElectricityUsageConfiguration : IEntityTypeConfiguration<ElectricityUsage>
{
    public void Configure(EntityTypeBuilder<ElectricityUsage> entity)
    {
        entity.ToTable("ElectricityUsages", "utilities");

        entity.Property(e => e.Id).ValueGeneratedNever();
        entity.Property(e => e.Units).HasColumnType("decimal(18, 2)");

        OnConfigurePartial(entity);
    }

    partial void OnConfigurePartial(EntityTypeBuilder<ElectricityUsage> entity);
}
