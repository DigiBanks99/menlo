using Microsoft.Azure.CosmosRepository;

namespace Menlo.Utilities.Models;

public partial class ElectricityPurchase : Item
{
    public required DateTimeOffset Date { get; init; }
    public required decimal Units { get; init; }
    public required decimal Cost { get; init; }
}
