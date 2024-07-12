namespace Menlo.Common;

public record DateRangeQuery(DateOnly StartDate, DateOnly? EndDate = null, TimeSpan TimeZone = default)
{
    public DateTimeOffset StartDateTimeOffset => new(StartDate.ToDateTime(TimeOnly.MinValue), TimeZone);
    public DateTimeOffset EndDateTimeOffset => new((EndDate ?? StartDate).ToDateTime(TimeOnly.MaxValue), TimeZone);
}

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class DateRangeParsableAttribute : Attribute { }
