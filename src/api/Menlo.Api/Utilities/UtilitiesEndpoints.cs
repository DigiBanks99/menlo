using Menlo.Utilities.Models;

namespace Menlo.Utilities;

public static class UtilitiesEndpoints
{
    public static void MapEndpoints(this WebApplication app)
    {
        app.MapGet("/utilities/electricity", () =>
        {
            List<ElectricityUsage> electricityUsages =
            [
                new ElectricityUsage { Id = 1, Units = 21, Date = new DateTimeOffset(2024, 7, 1, 5, 5, 0, TimeSpan.FromHours(2)) },
                new ElectricityUsage { Id = 2, Units = 25, Date = new DateTimeOffset(2024, 7, 2, 5, 5, 0, TimeSpan.FromHours(2)) },
                new ElectricityUsage { Id = 3, Units = 19, Date = new DateTimeOffset(2024, 7, 3, 5, 5, 0, TimeSpan.FromHours(2)) }
            ];

            return Results.Ok(electricityUsages);
        })
        .WithName("GetElectricityUsage")
        .WithOpenApi();
    }
}
