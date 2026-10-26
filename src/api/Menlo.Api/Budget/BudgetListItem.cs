namespace Menlo.Api.Budget;

/// <summary>
/// A budget as shown in the budget list, with the current month's expense figures.
/// </summary>
/// <param name="SpentExpenses">Null when nothing has been recorded as spent this month.</param>
public sealed record BudgetListItem(
    Guid Id,
    int Year,
    string Status,
    MoneyDto PlannedExpenses,
    MoneyDto? SpentExpenses,
    int CategoryCount);
