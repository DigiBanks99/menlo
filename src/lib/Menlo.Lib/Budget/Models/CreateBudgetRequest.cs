namespace Menlo.Lib.Budget.Models;

/// <summary>
/// Request model for creating a new budget.
/// </summary>
public sealed record CreateBudgetRequest(
    string Name,
    int Year,
    int Month,
    string Currency);
