using JetBrains.Annotations;
using Menlo.Common;
using Menlo.Common.Errors;
using Menlo.Utilities.Models;
using Microsoft.Azure.CosmosRepository;
using Microsoft.Extensions.Logging;

namespace Menlo.Utilities.Handlers.Water;

[PublicAPI]
public record CaptureWaterReadingCommand(DateOnly CaptureDate, int Units, bool IsMunicipalReading = false);

public sealed class CaptureWaterReadingHandler(
    ILogger<CaptureWaterReadingHandler> logger,
    IRepository<WaterReading> repo)
    : ICommandHandler<CaptureWaterReadingCommand, string>
{
    public async Task<Response<string, MenloError>> HandleAsync(CaptureWaterReadingCommand command, CancellationToken cancellationToken)
    {
        logger.HandlingCommandToCaptureWaterReading(command);

        WaterReading reading = new()
        {
            Date = command.CaptureDate, Units = command.Units, IsMunicipalReading = command.IsMunicipalReading
        };

        WaterReading savedReading = await repo.CreateAsync(reading, cancellationToken);

        return new Response<string, MenloError>() { Data = savedReading.Id };
    }
}
