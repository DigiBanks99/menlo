using Menlo.Common;
using Menlo.Common.Errors;
using Menlo.Utilities.Models;
using Microsoft.Azure.CosmosRepository;
using Microsoft.Extensions.Logging;

namespace Menlo.Utilities.Handlers.Electricity;

public record CaptureElectricityPurchaseRequest
{
    public required DateTimeOffset Date { get; init; }
    public required decimal Units { get; init; }
    public required decimal Cost { get; init; }
}

internal sealed class CaptureElectricityPurchaseHandler(
    ILogger<CaptureElectricityPurchaseHandler> logger,
    IRepository<ElectricityPurchase> repo)
    : ICommandHandler<CaptureElectricityPurchaseRequest, string>
{
    private readonly ILogger<CaptureElectricityPurchaseHandler> _logger = logger;
    private readonly IRepository<ElectricityPurchase> _repo = repo;

    public async Task<Response<string, MenloError>> HandleAsync(CaptureElectricityPurchaseRequest request, CancellationToken cancellationToken)
    {
        _logger.HandlingRequestToCaptureElectricityPurchase(request);

        ElectricityPurchase electricityPurchase = new()
        {
            Date = request.Date,
            Units = request.Units,
            Cost = request.Cost
        };

        ElectricityPurchase savedPurchase = await _repo.CreateAsync(electricityPurchase, cancellationToken);

        return new Response<string, MenloError>()
        {
            Data = savedPurchase.Id
        };
    }
}
