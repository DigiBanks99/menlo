namespace Menlo.Api.Budget;

/// <summary>
/// The body of <c>POST /api/budgets</c>: the calendar year to create a budget for.
/// </summary>
/// <param name="Year">The calendar year the budget covers.</param>
public sealed record CreateBudgetRequest(int Year);

/// <summary>
/// A budget: the household's plan for one calendar year.
/// </summary>
/// <param name="Id">The budget's ID.</param>
/// <param name="Year">The calendar year it covers.</param>
public sealed record BudgetDto(Guid Id, int Year);
