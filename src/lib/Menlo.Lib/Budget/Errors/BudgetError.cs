using Menlo.Lib.Budget.Entities;
using Menlo.Lib.Budget.ValueObjects;
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

/// <summary>
/// Error indicating a name that can't be a category's: it is blank, or longer than
/// <see cref="Category.MaxNameLength"/> characters.
/// </summary>
/// <param name="name">The name given.</param>
public sealed class InvalidCategoryNameError(string? name)
    : BudgetError("Budget.InvalidCategoryName", $"A category needs a name, of at most {Category.MaxNameLength} characters.")
{
    /// <summary>
    /// Gets the name given.
    /// </summary>
    public string? Name { get; } = name;
}

/// <summary>
/// Error indicating a category with the same name, ignoring case, already sits at the same place in the budget: at the
/// top level, or under the same parent.
/// </summary>
/// <param name="name">The name that is taken.</param>
public sealed class CategoryNameTakenError(string name)
    : BudgetError("Budget.CategoryNameTaken", $"There is already a category called \"{name}\" here.")
{
    /// <summary>
    /// Gets the name that is taken.
    /// </summary>
    public string Name { get; } = name;
}

/// <summary>
/// Error indicating a category would sit a third level deep, because its parent is itself a subcategory. Categories
/// nest at most two deep.
/// </summary>
/// <param name="parentId">The subcategory it was to sit under.</param>
public sealed class CategoryTooDeepError(CategoryId parentId)
    : BudgetError("Budget.CategoryTooDeep", "Categories nest at most two deep, so a subcategory can't have subcategories.")
{
    /// <summary>
    /// Gets the subcategory it was to sit under.
    /// </summary>
    public CategoryId ParentId { get; } = parentId;
}

/// <summary>
/// Error indicating the budget has no category with the ID given.
/// </summary>
/// <param name="categoryId">The ID given.</param>
public sealed class CategoryNotFoundError(CategoryId categoryId)
    : BudgetError("Budget.CategoryNotFound", "The budget has no such category.")
{
    /// <summary>
    /// Gets the ID given.
    /// </summary>
    public CategoryId CategoryId { get; } = categoryId;
}

/// <summary>
/// Error indicating the year has no budget.
/// </summary>
/// <param name="year">The year given.</param>
public sealed class BudgetNotFoundError(int year)
    : BudgetError("Budget.NotFound", $"{year} has no budget.")
{
    /// <summary>
    /// Gets the year given.
    /// </summary>
    public int Year { get; } = year;
}
