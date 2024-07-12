using Menlo.Common;
using Menlo.Common.Errors;
using Menlo.Utilities.Models;
using Microsoft.Azure.CosmosRepository;
using Microsoft.Extensions.Logging;

namespace Menlo.Utilities.Handlers.Electricity;

public record CaptureElectricityUsageRequest
{
    public required DateTimeOffset Date { get; init; }
    public required decimal Units { get; init; }
    public required ApplianceUsageInfo[] ApplianceUsages { get; init; }

    public record ApplianceUsageInfo(int ApplianceId, decimal HoursOfUse);
}

internal class CaptureElectricityUsageHandler(
    ILogger<CaptureElectricityUsageHandler> logger,
    IRepository<ElectricityUsage> repo)
    : ICommandHandler<CaptureElectricityUsageRequest, string>
{
    private readonly ILogger<CaptureElectricityUsageHandler> _logger = logger;
    private readonly IRepository<ElectricityUsage> _repo = repo;

    public async Task<Response<string, MenloError>> HandleAsync(CaptureElectricityUsageRequest request, CancellationToken cancellationToken)
    {
        // Capture electricity usage
        _logger.HandlingRequestToCaptureElectricityUsage(request);

        ElectricityUsage electricityUsage = new()
        {
            Date = request.Date,
            Units = request.Units,
            ApplianceUsages = request.ApplianceUsages.Select(applianceUsage => new ApplianceUsage
            {
                ApplianceId = applianceUsage.ApplianceId,
                HoursOfUse = applianceUsage.HoursOfUse,
                ElectricityUsageId = 0
            }).ToList()
        };

        ElectricityUsage savedElectricityUsage = await _repo.CreateAsync(electricityUsage, cancellationToken);

        return new Response<string, MenloError>()
        {
            Data = savedElectricityUsage.Id
        };
    }
}
