namespace Menlo.Api.Budget;

/// <summary>
/// The body of <c>POST /api/budgets</c>: the calendar year to create a budget for.
/// </summary>
/// <param name="Year">The calendar year the budget covers.</param>
public sealed record CreateBudgetRequest(int Year);

/// <summary>
/// The body of <c>POST /api/budgets/{year}/categories</c>: the new category's name, and the top-level category it sits
/// under, if any.
/// </summary>
/// <param name="Name">The category's name.</param>
/// <param name="ParentId">The ID of the top-level category it sits under, or <see langword="null"/> for a top-level
/// category.</param>
public sealed record AddCategoryRequest(string? Name, Guid? ParentId = null);

/// <summary>
/// A budget: the household's plan for one calendar year.
/// </summary>
/// <param name="Id">The budget's ID.</param>
/// <param name="Year">The calendar year it covers.</param>
/// <param name="Categories">Its top-level categories, by name, each with its subcategories.</param>
public sealed record BudgetDto(Guid Id, int Year, IReadOnlyList<CategoryDto> Categories);

/// <summary>
/// A budget's category.
/// </summary>
/// <param name="Id">The category's ID.</param>
/// <param name="Name">Its name.</param>
/// <param name="ParentId">The ID of the top-level category it sits under, or <see langword="null"/> when it is
/// top-level.</param>
/// <param name="Children">Its subcategories, by name. A subcategory has none.</param>
public sealed record CategoryDto(Guid Id, string Name, Guid? ParentId, IReadOnlyList<CategoryDto> Children);
