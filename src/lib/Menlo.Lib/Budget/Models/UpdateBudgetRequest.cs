namespace Menlo.Lib.Budget.Models;

/// <summary>
/// Request model for updating an existing budget.
/// </summary>
public sealed record UpdateBudgetRequest(
    string Name);
