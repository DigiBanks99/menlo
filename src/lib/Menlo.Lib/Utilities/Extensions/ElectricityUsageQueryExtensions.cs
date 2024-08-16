using Menlo.Utilities.Handlers.Electricity;

namespace Menlo.Utilities.Extensions;

internal static class ElectricityUsageQueryExtensions
{
    public static string GetCosmosQuery(this ElectricityUsageQuery query)
    {
        return
            $"""
            SELECT
              *
            FROM
              c
            WHERE
              c.date >= '{query.StartDateTimeOffset.AddDays(-1):yyyy-MM-ddTHH:mm:ssK}'
              AND c.date <= '{query.EndDateTimeOffset.AddDays(1):yyyy-MM-ddTHH:mm:ssK}'
            """;
    }

    public static string GetPurchaseCosmosQuery(this ElectricityUsageQuery query)
    {
        return
            $"""
            SELECT
              *
            FROM
              c
            WHERE
              c.date >= '{query.StartDateTimeOffset:yyyy-MM-ddTHH:mm:ssK}'
              AND c.date <= '{query.EndDateTimeOffset:yyyy-MM-ddTHH:mm:ssK}'
            """;
    }
}
