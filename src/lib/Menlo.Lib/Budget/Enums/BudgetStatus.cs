namespace Menlo.Lib.Budget.Enums;

/// <summary>
/// Represents the status of a budget.
/// </summary>
public enum BudgetStatus
{
    /// <summary>
    /// Budget is in draft status and can be modified.
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Budget has been activated and is in use.
    /// Active budgets have restricted modification capabilities.
    /// </summary>
    Active = 1
}
