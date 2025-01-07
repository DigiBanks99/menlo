using Menlo.Common;
using Menlo.Common.Errors;
using Menlo.Utilities.Handlers.Electricity;
using Microsoft.OpenApi.Models;
using System.Globalization;
using System.Net.Mime;

namespace Menlo.Utilities;

public static partial class ElectricityEndpoints
{
    public static void MapElectricityEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("", HandleUsageQuery)
            .WithName("GetElectricityUsage")
            .Produces<IEnumerable<ElectricityUsageQueryResponse>>(contentType: MediaTypeNames.Application.Json)
            .WithDateRangeQuery();

        routes.MapPost("usage", HandleUsageCaptureCommand)
            .WithName("CaptureElectricityUsage")
            .Produces<Guid>(StatusCodes.Status201Created, contentType: MediaTypeNames.Text.Plain);

        routes.MapPost("purchase", HandlePurchaseCaptureCommand)
            .WithName("CaptureElectricityPurchase")
            .Produces<Guid>(StatusCodes.Status201Created, contentType: MediaTypeNames.Text.Plain);
    }

    private static async Task<IResult> HandleUsageQuery(
        IQueryHandler<ElectricityUsageQuery, IEnumerable<ElectricityUsageQueryResponse>> handler,
        HttpRequest request,
        CancellationToken requestAborted)
    {
        var query = ElectricityUsageQuery.Parse(request.QueryString.Value ?? string.Empty, CultureInfo.CurrentCulture);
        Response<IEnumerable<ElectricityUsageQueryResponse>, MenloError> response = await handler
            .HandleAsync(query, requestAborted);
        return response.IsSuccess
            ? Results.Ok(response.Data)
            : Results.Problem();
    }

    private static async Task<IResult> HandleUsageCaptureCommand(
        ICommandHandler<CaptureElectricityUsageRequest, string> handler,
        CaptureElectricityUsageRequest request,
        CancellationToken requestAborted)
    {
        Response<string, MenloError> response = await handler.HandleAsync(request, requestAborted);
        return response.IsSuccess
            ? Results.Created($"/utilities/electricity/{response.Data}", response.Data)
            : Results.Problem();
    }

    private static async Task<IResult> HandlePurchaseCaptureCommand(
        ICommandHandler<CaptureElectricityPurchaseRequest, string> handler,
        CaptureElectricityPurchaseRequest request,
        CancellationToken requestAborted)
    {
        Response<string, MenloError> response = await handler.HandleAsync(request, requestAborted);
        return response.IsSuccess
            ? Results.Created($"/utilities/electricity/{response.Data}", response.Data)
            : Results.Problem();
    }
}
