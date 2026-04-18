namespace Menlo.Lib.Budget.Enums;

/// <summary>
/// Represents the lifecycle status of a budget.
/// </summary>
public enum BudgetStatus
{
    /// <summary>
    /// The budget is being set up and has not yet been activated.
    /// </summary>
    Draft,

    /// <summary>
    /// The budget is the live plan for its year.
    /// </summary>
    Active,

    /// <summary>
    /// The budget has been closed and can no longer be modified.
    /// </summary>
    Closed,
}
