using Menlo.Utilities.Models;
using Microsoft.EntityFrameworkCore;

namespace Menlo.Utilities;

internal partial class UtilitiesDbContext(DbContextOptions<UtilitiesDbContext> options) : DbContext(options)
{
    public DbSet<Appliance> Appliances { get; private set; }
    public DbSet<ElectricityUsage> ElectricityUsages { get; private set; }
    public DbSet<ApplianceUsage> ApplianceUsages { get; private set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new Configurations.ApplianceConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.ApplianceUsageConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.ElectricityUsageConfiguration());

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
