namespace Menlo.Lib.Budget.Enums;

/// <summary>
/// Indicates whether a category accepts income items, expense items, or both.
/// </summary>
public enum BudgetFlow
{
    /// <summary>Category accepts income items only.</summary>
    Income,

    /// <summary>Category accepts expense items only.</summary>
    Expense,

    /// <summary>Category accepts both income and expense items.</summary>
    Both,
}
