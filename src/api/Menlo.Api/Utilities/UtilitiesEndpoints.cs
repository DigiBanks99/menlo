using Menlo.Common;
using Menlo.Common.Errors;
using Menlo.Utilities.Handlers.Electricity;
using Microsoft.OpenApi.Models;
using System.Globalization;
using System.Net.Mime;

namespace Menlo.Utilities;

public static partial class UtilitiesEndpoints
{
    public static void MapEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapPost(
                "/utilities/electricity/usage",
                async (
                    ICommandHandler<CaptureElectricityUsageRequest, string> handler,
                    CaptureElectricityUsageRequest request,
                    CancellationToken requestAborted) =>
                {
                    Response<string, MenloError> response = await handler.HandleAsync(request, requestAborted);
                    return response.IsSuccess
                        ? Results.Created($"/utilities/electricity/{response.Data}", response.Data)
                        : Results.Problem();
                })
            .WithName("CaptureElectricityUsage")
            .Produces<Guid>(StatusCodes.Status201Created, contentType: MediaTypeNames.Text.Plain)
            .WithOpenApi();

        routes.MapPost(
                "/utilities/electricity/purchase",
                async (
                    ICommandHandler<CaptureElectricityPurchaseRequest, string> handler,
                    CaptureElectricityPurchaseRequest request,
                    CancellationToken requestAborted) =>
                {
                    Response<string, MenloError> response = await handler.HandleAsync(request, requestAborted);
                    return response.IsSuccess
                        ? Results.Created($"/utilities/electricity/{response.Data}", response.Data)
                        : Results.Problem();
                })
            .WithName("CaptureElectricityPurchase")
            .Produces<Guid>(StatusCodes.Status201Created, contentType: MediaTypeNames.Text.Plain)
            .WithOpenApi();

        routes.MapGet(
                "/utilities/electricity",
                async (
                    IQueryHandler<ElectricityUsageQuery, IEnumerable<ElectricityUsageQueryResponse>> handler,
                    HttpRequest request,
                    CancellationToken requestAborted) =>
                {
                    ElectricityUsageQuery? query = ElectricityUsageQuery.Parse(request.QueryString.Value ?? string.Empty, CultureInfo.CurrentCulture);
                    if (query is null)
                    {
                        ProblemDetails problem = new()
                        {
                            Title = "Invalid query",
                            Detail = "The query string could not be parsed. The following minimum query string is required startDate=<yyyy-MM-dd>",
                            Status = StatusCodes.Status400BadRequest,
                            Type = "https://httpstatuses.com/400",
                        };
                        return Results.BadRequest(problem);
                    }
                    Response<IEnumerable<ElectricityUsageQueryResponse>, MenloError> response = await handler
                        .HandleAsync(query, requestAborted);
                    return response.IsSuccess
                        ? Results.Ok(response.Data)
                        : Results.Problem();
                })
            .WithName("GetElectricityUsage")
            .Produces<IEnumerable<ElectricityUsageQueryResponse>>(contentType: MediaTypeNames.Application.Json)
            .WithOpenApi(o =>
            {
                o.Parameters.Add(new OpenApiParameter
                {
                    Name = "startDate",
                    AllowEmptyValue = false,
                    Description = "The start date of the usages that must be returned",
                    In = ParameterLocation.Query,
                    Required = true,
                    Schema = new() { Format = "date", Type = "string" }
                });

                o.Parameters.Add(new OpenApiParameter
                {
                    Name = "endDate",
                    AllowEmptyValue = true,
                    Description = "The end date of the usages that must be returned",
                    In = ParameterLocation.Query,
                    Required = false,
                    Schema = new() { Format = "date", Type = "string" }
                });

                o.Parameters.Add(new OpenApiParameter
                {
                    Name = "timeZone",
                    AllowEmptyValue = true,
                    Description = "The time zone of the client",
                    In = ParameterLocation.Query,
                    Required = false,
                    Schema = new() { Format = "timestamp", Type = "string", Pattern = TimeZonePatternRegex().ToString() }
                });

                return o;
            });
    }

    [System.Text.RegularExpressions.GeneratedRegex(@"[+-]{1}\d2:\d2")]
    private static partial System.Text.RegularExpressions.Regex TimeZonePatternRegex();
}
