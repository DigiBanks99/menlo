using Menlo.Common;
using Menlo.Common.Errors;
using Menlo.Utilities.Handlers.Water;
using Menlo.Utilities.Models;
using System.Globalization;
using System.Net.Mime;

namespace Menlo.Utilities;

public static class WaterEndpoints
{
    public static void MapWaterEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("reading", HandleCaptureWaterReading)
            .WithName("CaptureWaterReading")
            .Produces<string>(StatusCodes.Status201Created, contentType: MediaTypeNames.Application.Json);

        routes.MapGet("reading", HandleWaterReadingQuery)
            .WithName("Query water reading")
            .Produces<IEnumerable<WaterReading>>(contentType: MediaTypeNames.Application.Json)
            .WithDateRangeQuery();
    }

    private static async Task<IResult> HandleCaptureWaterReading(
        ICommandHandler<CaptureWaterReadingCommand, string> handler,
        CaptureWaterReadingCommand command,
        CancellationToken requestAborted)
    {
        Response<string, MenloError> response = await handler.HandleAsync(command, requestAborted);
        return response.IsSuccess
            ? Results.Created($"/utilities/water/{response.Data}", response.Data)
            : Results.Problem();
    }

    private static async Task<IResult> HandleWaterReadingQuery(
        IQueryHandler<WaterReadingQuery, IEnumerable<WaterReading>> handler,
        HttpRequest request,
        CancellationToken requestAborted)
    {
        var query = WaterReadingQuery.Parse(request.QueryString.Value ?? string.Empty, CultureInfo.CurrentCulture);
        Response<IEnumerable<WaterReading>, MenloError> response = await handler.HandleAsync(query, requestAborted);
        return response.IsSuccess
            ? Results.Ok(response.Data)
            : Results.Problem();
    }
}
