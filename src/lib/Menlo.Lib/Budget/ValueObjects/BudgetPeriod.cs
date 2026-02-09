using CSharpFunctionalExtensions;
using Menlo.Lib.Budget.Errors;

namespace Menlo.Lib.Budget.ValueObjects;

/// <summary>
/// Represents a budget period (year and month combination).
/// </summary>
/// <param name="Year">The year of the budget period.</param>
/// <param name="Month">The month of the budget period (1-12).</param>
public readonly record struct BudgetPeriod(int Year, int Month)
{
    /// <summary>
    /// Creates a new BudgetPeriod with validation.
    /// </summary>
    /// <param name="year">The year of the budget period.</param>
    /// <param name="month">The month of the budget period (1-12).</param>
    /// <returns>Success with BudgetPeriod if valid; Failure with error otherwise.</returns>
    public static Result<BudgetPeriod, BudgetError> Create(int year, int month)
    {
        if (year < 1900 || year > 2100)
        {
            return new InvalidPeriodError($"Year must be between 1900 and 2100, got {year}.");
        }

        if (month < 1 || month > 12)
        {
            return new InvalidPeriodError($"Month must be between 1 and 12, got {month}.");
        }

        return new BudgetPeriod(year, month);
    }

    /// <summary>
    /// Returns the string representation of the period in YYYY-MM format.
    /// </summary>
    public override string ToString() => $"{Year:D4}-{Month:D2}";
}
