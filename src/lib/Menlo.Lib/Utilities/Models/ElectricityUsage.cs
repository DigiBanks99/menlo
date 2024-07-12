using Microsoft.Azure.CosmosRepository;

namespace Menlo.Utilities.Models;

public partial class ElectricityUsage : Item
{
    public required DateTimeOffset Date { get; init; }
    public required decimal Units { get; init; }

    public virtual ICollection<ApplianceUsage>? ApplianceUsages { get; init; }
}
