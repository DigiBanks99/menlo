using Menlo.Common;
using Menlo.Common.Errors;
using Menlo.Utilities.Extensions;
using Menlo.Utilities.Models;
using Microsoft.Azure.CosmosRepository;
using Microsoft.Extensions.Logging;

namespace Menlo.Utilities.Handlers.Electricity;

[DateRangeParsable]
public partial record ElectricityUsageQuery;

internal class ElectricityUsageQueryHandler(ILogger<ElectricityUsageQueryHandler> logger, IRepository<ElectricityUsage> repo)
    : IQueryHandler<ElectricityUsageQuery, IEnumerable<ElectricityUsageQueryResponse>>
{
    private readonly ILogger<ElectricityUsageQueryHandler> _logger = logger;
    private readonly IRepository<ElectricityUsage> _repo = repo;

    public async Task<Response<IEnumerable<ElectricityUsageQueryResponse>, MenloError>> HandleAsync(
        ElectricityUsageQuery query,
        CancellationToken cancellationToken)
    {
        _logger.HandlingElectricityUsageQuery(query);

        IEnumerable<ElectricityUsage> usages = await _repo.GetByQueryAsync(query.GetCosmosQuery(), cancellationToken);

        return new Response<IEnumerable<ElectricityUsageQueryResponse>, MenloError>()
        {
            Data = usages.Select(ElectricityUsageQueryResponse.From)
        };
    }
}

public record class ElectricityUsageQueryResponse
{
    public DateTimeOffset Date { get; private set; }
    public decimal Units { get; private set; }
    public IEnumerable<ApplianceUsageQueryResponse> ApplianceUsages { get; private set; } = [];

    internal static ElectricityUsageQueryResponse From(ElectricityUsage usage)
    {
        return new ElectricityUsageQueryResponse
        {
            Date = usage.Date,
            Units = usage.Units,
            ApplianceUsages = usage.ApplianceUsages?.Select(ApplianceUsageQueryResponse.From) ?? []
        };
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
}
