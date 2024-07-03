namespace Menlo.Lib.Utilities.Models;

public partial class ElectricityUsage
{
    public required int Id { get; init; }
    public required DateTimeOffset Date { get; init; }
    public required decimal Units { get; init; }

    public virtual ICollection<ApplianceUsage>? ApplianceUsages { get; init; }
}
