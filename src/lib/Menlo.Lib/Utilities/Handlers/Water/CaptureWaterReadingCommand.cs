using JetBrains.Annotations;
using Menlo.Common;
using Menlo.Common.Errors;
using Menlo.Utilities.Models;
using Menlo.Utilities.Specifications;
using Microsoft.Azure.CosmosRepository;
using Microsoft.Extensions.Logging;

namespace Menlo.Utilities.Handlers.Water;

[PublicAPI]
public record CaptureWaterReadingCommand(DateOnly CaptureDate, decimal Reading, SimpleMoney Cost);

public sealed class CaptureWaterReadingHandler(
    ILogger<CaptureWaterReadingHandler> logger,
    IRepository<WaterReading> repo)
    : ICommandHandler<CaptureWaterReadingCommand, string>
{
    public async Task<Response<string, MenloError>> HandleAsync(CaptureWaterReadingCommand command,
        CancellationToken cancellationToken)
    {
        logger.HandlingCommandToCaptureWaterReading(command);

        IEnumerable<WaterReading> previousReadings = await repo
            .GetByQueryAsync(PreviousWaterReadingQuery.Create(command.CaptureDate), cancellationToken);
        WaterReading? previousReading = previousReadings.OrderByDescending(r => r.Date).FirstOrDefault();
        WaterReading.Reading ownReading = new(
            previousReading?.OwnReading.Closing ?? LiterMeasurement.Zero,
            new LiterMeasurement(command.Reading),
            command.Cost.ToMoney());
        WaterReading reading = new(ownReading, command.CaptureDate);

        WaterReading savedReading = await repo.CreateAsync(reading, cancellationToken);

        return new Response<string, MenloError>() { Data = savedReading.Id };
    }
}
