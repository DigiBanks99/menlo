using Menlo.Lib.Common.Abstractions;

namespace Menlo.Lib.Budget.Errors;

/// <summary>
/// Base class for budget domain errors.
/// </summary>
/// <param name="code">Machine-readable error code.</param>
/// <param name="message">Human-readable error message.</param>
public class BudgetError(string code, string message) : Error(code, message);

/// <summary>
/// Error indicating a year that can't be a budget's calendar year.
/// </summary>
/// <param name="year">The year given.</param>
public sealed class InvalidBudgetYearError(int year)
    : BudgetError("Budget.InvalidYear", $"{year} isn't a budget year.")
{
    /// <summary>
    /// Gets the year given.
    /// </summary>
    public int Year { get; } = year;
}

/// <summary>
/// Error indicating the year already has a budget. There is one budget per year.
/// </summary>
/// <param name="year">The year that already has a budget.</param>
public sealed class BudgetYearTakenError(int year)
    : BudgetError("Budget.YearTaken", $"{year} already has a budget.")
{
    /// <summary>
    /// Gets the year that already has a budget.
    /// </summary>
    public int Year { get; } = year;
}
