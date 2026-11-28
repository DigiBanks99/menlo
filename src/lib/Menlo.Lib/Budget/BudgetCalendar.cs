namespace Menlo.Lib.Budget;

/// <summary>
/// Where a month of a budget year stands against today.
/// </summary>
public enum BudgetMonthState
{
    /// <summary>A month before the current month. Its planned amounts can no longer change.</summary>
    Frozen,

    /// <summary>The current month. It is still open to change.</summary>
    Current,

    /// <summary>A month after the current month.</summary>
    Ahead
}

/// <summary>
/// The budget's calendar. The current month is by South African time (Africa/Johannesburg), so a month freezes at
/// local midnight on the 1st.
/// </summary>
public static class BudgetCalendar
{
    /// <summary>The time zone the household lives in.</summary>
    public static TimeZoneInfo Zone { get; } = FindZone();

    /// <summary>
    /// The current month, as the first day of that month in South Africa.
    /// </summary>
    /// <param name="now">The present moment, from a <see cref="TimeProvider"/>.</param>
    public static DateOnly CurrentMonth(DateTimeOffset now)
    {
        DateTimeOffset local = TimeZoneInfo.ConvertTime(now, Zone);
        return new DateOnly(local.Year, local.Month, 1);
    }

    /// <summary>
    /// Where a month of a year stands against the current month.
    /// </summary>
    /// <param name="year">The calendar year.</param>
    /// <param name="month">The month, 1 to 12.</param>
    /// <param name="currentMonth">The current month, from <see cref="CurrentMonth"/>.</param>
    public static BudgetMonthState StateOf(int year, int month, DateOnly currentMonth)
    {
        int compared = new DateOnly(year, month, 1).CompareTo(new DateOnly(currentMonth.Year, currentMonth.Month, 1));
        return compared switch
        {
            < 0 => BudgetMonthState.Frozen,
            0 => BudgetMonthState.Current,
            _ => BudgetMonthState.Ahead
        };
    }

    // South Africa has kept UTC+2 with no daylight saving since 1944, so a fixed zone stands in where the host has
    // no time zone data
    private static TimeZoneInfo FindZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Africa/Johannesburg");
        }
        catch (Exception exception) when (exception is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return TimeZoneInfo.CreateCustomTimeZone("Africa/Johannesburg", TimeSpan.FromHours(2), "South Africa", "SAST");
        }
    }
}
