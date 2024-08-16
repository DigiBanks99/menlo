using Menlo.Utilities.Models;
using System.Collections.Immutable;

namespace Menlo.Utilities.Handlers.Electricity;

public record class ElectricityUsageQueryResponseBuilder
{
    public required ElectricityUsage CurrentUsageRecord { get; init; }
    public ElectricityUsage? PreviousUsageRecord { get; init; }
    public ElectricityPurchase? PurchaseRecord { get; init; }

    public ElectricityUsageQueryResponse Build()
    {
        List<ApplianceUsageQueryResponse>? applianceUsageQueryResponses = CurrentUsageRecord.ApplianceUsages?.Select(ApplianceUsageQueryResponse.From).ToList() ?? [];

        decimal previousUnits = PreviousUsageRecord?.Units ?? 0;
        decimal purchaseUnits = PurchaseRecord?.Units ?? 0;
        decimal usage = Math.Abs(previousUnits + purchaseUnits - CurrentUsageRecord.Units);
        return new ElectricityUsageQueryResponse()
        {
            Date = CurrentUsageRecord.Date,
            Units = CurrentUsageRecord.Units,
            Usage = usage,
            ApplianceUsages = applianceUsageQueryResponses.ToImmutableList()
        };
    }
}
