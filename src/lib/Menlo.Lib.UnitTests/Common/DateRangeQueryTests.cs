using Shouldly;

namespace Menlo.Common;

public class DateRangeQueryTests
{
    [Fact]
    public void StartDateTimeOffset_ShouldReturn_CorrectDateTimeOffset_WhenOnlyStartDateIsProvided()
    {
        // Arrange
        DateOnly startDate = new(2024, 1, 1);
        DateRangeQuery dateRangeQuery = new(startDate);

        // Assert
        dateRangeQuery.StartDateTimeOffset.ShouldBe(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero));
    }

    [Theory]
    [InlineData(2024, 1, 1, null, null, null, "02:00:00")]
    [InlineData(2024, 2, 29, null, null, null, "-04:00:00")]
    [InlineData(2024, 12, 31, null, null, null, "12:00:00")]
    public void StartDateTimeOffset_ShouldReturn_CorrectDateTimeOffset_WhenTimeZoneIsProvided(
        int startDateYear,
        int startDateMonth,
        int startDateDay,
        int? endDateYear,
        int? endDateMonth,
        int? endDateDay,
        string timeZone)
    {
        // Arrange
        DateOnly startDate = new(startDateYear, startDateMonth, startDateDay);
        DateOnly? endDate = endDateYear is null || endDateMonth is null || endDateDay is null
            ? null
            : new DateOnly(endDateYear.Value, endDateMonth.Value, endDateDay.Value);
        TimeSpan timeZoneParsed = TimeSpan.Parse(timeZone);
        DateRangeQuery dateRangeQuery = new(startDate, endDate, timeZoneParsed);

        // Assert
        dateRangeQuery.StartDateTimeOffset.ShouldBe(new DateTimeOffset(startDate.ToDateTime(TimeOnly.MinValue), timeZoneParsed));
    }

    [Theory]
    [InlineData(2024, 1, 1, null, null, null, "02:00:00")]
    [InlineData(2024, 2, 29, null, null, null, "-04:00:00")]
    [InlineData(2024, 12, 31, null, null, null, "12:00:00")]
    [InlineData(2024, 1, 1, 2024, 2, 23, "02:00:00")]
    public void EndDateTimeOffset_ShouldReturn_CorrectDateTimeOffset(
        int startDateYear,
        int startDateMonth,
        int startDateDay,
        int? endDateYear,
        int? endDateMonth,
        int? endDateDay,
        string timeZone)
    {
        // Arrange
        DateOnly startDate = new(startDateYear, startDateMonth, startDateDay);
        DateOnly? endDate = endDateYear is null || endDateMonth is null || endDateDay is null
            ? null
            : new DateOnly(endDateYear.Value, endDateMonth.Value, endDateDay.Value);
        TimeSpan timeZoneParsed = TimeSpan.Parse(timeZone);
        DateRangeQuery dateRangeQuery = new(startDate, endDate, timeZoneParsed);

        // Assert
        dateRangeQuery.EndDateTimeOffset.ShouldBe(new DateTimeOffset((endDate ?? startDate).ToDateTime(TimeOnly.MaxValue), timeZoneParsed));
    }
}
