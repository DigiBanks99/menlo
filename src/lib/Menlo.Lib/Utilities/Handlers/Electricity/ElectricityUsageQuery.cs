using Menlo.Common;
using Menlo.Common.Errors;
using Menlo.Utilities.Extensions;
using Menlo.Utilities.Models;
using Microsoft.Azure.CosmosRepository;
using Microsoft.Extensions.Logging;
using System.Collections.Immutable;

namespace Menlo.Utilities.Handlers.Electricity;

[DateRangeParsable]
public partial record ElectricityUsageQuery;

internal class ElectricityUsageQueryHandler(ILogger<ElectricityUsageQueryHandler> logger, IRepository<ElectricityUsage> repoUsages, IRepository<ElectricityPurchase> repoPurchases)
    : IQueryHandler<ElectricityUsageQuery, IEnumerable<ElectricityUsageQueryResponse>>
{
    private readonly ILogger<ElectricityUsageQueryHandler> _logger = logger;
    private readonly IRepository<ElectricityUsage> _repoUsages = repoUsages;
    private readonly IRepository<ElectricityPurchase> _repoPurchases = repoPurchases;

    public async Task<Response<IEnumerable<ElectricityUsageQueryResponse>, MenloError>> HandleAsync(
        ElectricityUsageQuery query,
        CancellationToken cancellationToken)
    {
        _logger.HandlingElectricityUsageQuery(query);

        IEnumerable<ElectricityUsage> usages = await _repoUsages.GetByQueryAsync(query.GetCosmosQuery(), cancellationToken);
        IEnumerable<ElectricityPurchase> purchases = await _repoPurchases.GetByQueryAsync(query.GetPurchaseCosmosQuery(), cancellationToken);

        usages = usages.OrderByDescending(usage => usage.Date);
        purchases = purchases.OrderByDescending(purchase => purchase.Date);

        List<ElectricityUsageQueryResponse> data = [];
        ElectricityUsageQueryResponseBuilder builder;
        foreach (ElectricityUsage usage in usages)
        {
            // Possibly optimize this.
            // We could use a queue to store the previous usage records and pop them off as we iterate through the usage records.
            ElectricityUsage? previousUsage = usages.FirstOrDefault(u => u.Date == usage.Date.AddDays(-1));
            ElectricityPurchase? purchase = purchases.FirstOrDefault(p => p.Date == usage.Date.AddDays(-1));

            builder = new ElectricityUsageQueryResponseBuilder
            {
                CurrentUsageRecord = usage,
                PreviousUsageRecord = previousUsage,
                PurchaseRecord = purchase
            };

            ElectricityUsageQueryResponse queryResponse = builder.Build();
            data.Add(queryResponse);
        }

        return new Response<IEnumerable<ElectricityUsageQueryResponse>, MenloError>()
        {
            Data = data
            .OrderBy(item => item.Date)
            .Where(item => item.Date >= query.StartDateTimeOffset && item.Date <= query.EndDateTimeOffset)
            .ToImmutableList()
        };
    }
}

public record class ElectricityUsageQueryResponse
{
    public required DateTimeOffset Date { get; init; }
    public required decimal Units { get; init; }
    public required decimal Usage { get; init; }

    public required IImmutableList<ApplianceUsageQueryResponse> ApplianceUsages { get; init; }
}

public class ApplianceUsageQueryResponse
{
    public int ApplianceId { get; private set; }
    public decimal HoursOfUse { get; private set; }

    internal static ApplianceUsageQueryResponse From(ApplianceUsage applianceUsage)
    {
        return new ApplianceUsageQueryResponse
        {
            ApplianceId = applianceUsage.ApplianceId,
            HoursOfUse = applianceUsage.HoursOfUse
        };
    }
}
