using Microsoft.OpenApi.Models;

namespace Menlo.Common;

public static partial class OpenApiExtensions
{
    [System.Text.RegularExpressions.GeneratedRegex(@"[+-]{1}\d2:\d2")]
    private static partial System.Text.RegularExpressions.Regex TimeZonePatternRegex();

    public static RouteHandlerBuilder WithDateRangeQuery(this RouteHandlerBuilder route)
    {
        return route.WithOpenApi(o =>
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
                Schema = new()
                {
                    Format = "timestamp", Type = "string", Pattern = TimeZonePatternRegex().ToString()
                }
            });

            return o;
        });
    }
}
