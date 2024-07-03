namespace Menlo.Lib.Utilities.Models;

public partial class ApplianceUsage
{
    public required int ApplianceId { get; init; }
    public required int ElectricityUsageId { get; init; }
    public required decimal HoursOfUse { get; init; }

    public virtual Appliance? Appliance { get; init; }
    public virtual ElectricityUsage? ElectricityUsage { get; init; }
}
