using Microsoft.Azure.Cosmos;

namespace Menlo.Utilities.Specifications;

public static class PreviousWaterReadingQuery
{
    public static QueryDefinition Create(DateOnly currentDate)
    {
        return new QueryDefinition("SELECT * FROM c WHERE c.date < @currentDate AND c.date >= @previousDate")
            .WithParameter("@previousDate", currentDate.AddMonths(-1).ToString("yyyy-MM"))
            .WithParameter("@currentDate", currentDate.ToString("yyyy-MM"));
    }
}