namespace Menlo.Lib.Utilities.Models;

public partial class Appliance
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required DateOnly PurchaseDate { get; init; }

    public virtual ICollection<ApplianceUsage>? ApplianceUsages { get; set; }
}
