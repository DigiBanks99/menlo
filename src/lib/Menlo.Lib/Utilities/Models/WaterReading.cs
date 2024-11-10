using Microsoft.Azure.CosmosRepository;
using System.Diagnostics;

namespace Menlo.Utilities.Models;

[DebuggerDisplay("{Date.ToStringString(\"f\")} - {Units}{IsMunicipalReading ? \"Municipal\" : string.Empty}")]
public class WaterReading : Item
{
    public required DateOnly Date { get; init; }
    public required int Units { get; init; }
    public required bool IsMunicipalReading { get; init; } = false;
}
