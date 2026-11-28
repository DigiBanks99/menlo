using Menlo.Lib.Budget;
using Shouldly;

namespace Menlo.Lib.Tests.Budget;

public sealed class BudgetCalendarTests
{
    [Test]
    public void ItIsTheLastEveningOfAMonthInUtcButAlreadyTheFirstInSouthAfrica()
    {
        DateOnly current = BudgetCalendar.CurrentMonth(new DateTimeOffset(2026, 6, 30, 22, 30, 0, TimeSpan.Zero));

        ItShouldBeTheNewMonth();

        void ItShouldBeTheNewMonth() => current.ShouldBe(new DateOnly(2026, 7, 1));
    }

    [Test]
    public void ItIsJustBeforeMidnightOnTheLastDayInSouthAfrica()
    {
        DateOnly current = BudgetCalendar.CurrentMonth(new DateTimeOffset(2026, 6, 30, 21, 59, 59, TimeSpan.Zero));

        ItShouldStillBeTheOldMonth();

        void ItShouldStillBeTheOldMonth() => current.ShouldBe(new DateOnly(2026, 6, 1));
    }

    [Test]
    [Arguments(2026, 6, BudgetMonthState.Frozen)]
    [Arguments(2026, 7, BudgetMonthState.Current)]
    [Arguments(2026, 8, BudgetMonthState.Ahead)]
    [Arguments(2025, 12, BudgetMonthState.Frozen)]
    [Arguments(2027, 1, BudgetMonthState.Ahead)]
    public void AMonthIsComparedWithTheCurrentMonth(int year, int month, BudgetMonthState expected)
    {
        BudgetMonthState state = BudgetCalendar.StateOf(year, month, new DateOnly(2026, 7, 15));

        ItShouldStandWhereItFallsAgainstJuly2026();

        void ItShouldStandWhereItFallsAgainstJuly2026() => state.ShouldBe(expected);
    }
}
