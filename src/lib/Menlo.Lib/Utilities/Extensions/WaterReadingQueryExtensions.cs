using Menlo.Utilities.Handlers.Water;

namespace Menlo.Utilities.Extensions;

internal static class WaterReadingQueryExtensions
{
    public static string GetCosmosQuery(this WaterReadingQuery query)
    {
        return
            $"""
             SELECT
                *
             FROM
                c
             WHERE
                c.date > '{query.StartDateTimeOffset.AddDays(-1):yyyy-MM-dd}'
                AND c.date < '{query.EndDateTimeOffset.AddDays(+1):yyyy-MM-dd}'
             """;
    }
}
