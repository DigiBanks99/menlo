using Menlo.Common;
using Menlo.Common.Errors;
using Menlo.Utilities.Extensions;
using Menlo.Utilities.Models;
using Microsoft.Azure.CosmosRepository;
using Microsoft.Azure.CosmosRepository.Extensions;
using Microsoft.Extensions.Logging;
using System.Collections.Immutable;

namespace Menlo.Utilities.Handlers.Water;

[DateRangeParsable]
public partial record WaterReadingQuery;

internal class WaterReadingQueryHandler(ILogger<WaterReadingQueryHandler> logger, IRepository<WaterReading> repository)
    : IQueryHandler<WaterReadingQuery, IEnumerable<WaterReading>>
{
    public async Task<Response<IEnumerable<WaterReading>, MenloError>> HandleAsync(
        WaterReadingQuery query,
        CancellationToken cancellationToken)
    {
        logger.HandlingWaterReadingQuery(query);

        List<WaterReading> waterReadings = await repository
            .GetByQueryAsync(query.GetCosmosQuery(), cancellationToken)
            .ToListAsync();

        return new Response<IEnumerable<WaterReading>, MenloError>() { Data = waterReadings.ToImmutableList() };
    }
}
