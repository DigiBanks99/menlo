using Microsoft.Azure.CosmosRepository;
using System.Diagnostics;

namespace Menlo.Utilities.Models;

[DebuggerDisplay("{Date.ToString(\"f\")} - {Units}")]
public partial class ElectricityUsage : Item
{
    public required DateTimeOffset Date { get; init; }
    public required decimal Units { get; init; }

    public virtual ICollection<ApplianceUsage>? ApplianceUsages { get; init; }
}
