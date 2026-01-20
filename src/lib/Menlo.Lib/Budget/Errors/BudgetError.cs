using Menlo.Lib.Common.Abstractions;

namespace Menlo.Lib.Budget.Errors;

/// <summary>
/// Base class for budget domain errors.
/// </summary>
/// <param name="code">Machine-readable error code.</param>
/// <param name="message">Human-readable error message.</param>
public class BudgetError(string code, string message) : Error(code, message);

/// <summary>
/// Error indicating invalid budget data during creation.
/// </summary>
/// <param name="reason">The reason the data is invalid.</param>
public class InvalidBudgetDataError(string reason)
    : BudgetError("Budget.InvalidData", $"Invalid budget data: {reason}")
{
    /// <summary>
    /// Gets the reason the data is invalid.
    /// </summary>
    public string Reason { get; } = reason;
}

/// <summary>
/// Error indicating an invalid budget period.
/// </summary>
/// <param name="reason">The reason the period is invalid.</param>
public class InvalidPeriodError(string reason)
    : BudgetError("Budget.InvalidPeriod", reason)
{
    /// <summary>
    /// Gets the reason the period is invalid.
    /// </summary>
    public string Reason { get; } = reason;
}

/// <summary>
/// Error indicating a duplicate budget already exists for the period.
/// </summary>
/// <param name="name">The budget name.</param>
/// <param name="period">The budget period.</param>
public class DuplicateBudgetError(string name, string period)
    : BudgetError("Budget.Duplicate", $"A budget named '{name}' already exists for period {period}.")
{
    /// <summary>
    /// Gets the budget name.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Gets the budget period.
    /// </summary>
    public string Period { get; } = period;
}

/// <summary>
/// Error indicating a duplicate category name within the same parent scope.
/// </summary>
/// <param name="categoryName">The duplicate category name.</param>
public class DuplicateCategoryNameError(string categoryName)
    : BudgetError("Budget.DuplicateCategoryName", $"A category named '{categoryName}' already exists at this level.")
{
    /// <summary>
    /// Gets the duplicate category name.
    /// </summary>
    public string CategoryName { get; } = categoryName;
}

/// <summary>
/// Error indicating the category hierarchy exceeded maximum depth.
/// </summary>
public class MaxDepthExceededError()
    : BudgetError("Budget.MaxDepthExceeded", "Category hierarchy cannot exceed 2 levels (root and one level of subcategories).");

/// <summary>
/// Error indicating a category was not found.
/// </summary>
/// <param name="categoryId">The category ID that was not found.</param>
public class CategoryNotFoundError(Guid categoryId)
    : BudgetError("Budget.CategoryNotFound", $"Category with ID '{categoryId}' was not found.")
{
    /// <summary>
    /// Gets the category ID that was not found.
    /// </summary>
    public Guid CategoryId { get; } = categoryId;
}

/// <summary>
/// Error indicating a category cannot be deleted because it has children.
/// </summary>
/// <param name="categoryId">The category ID.</param>
public class CategoryHasChildrenError(Guid categoryId)
    : BudgetError("Budget.CategoryHasChildren", $"Category '{categoryId}' cannot be deleted because it has subcategories.")
{
    /// <summary>
    /// Gets the category ID.
    /// </summary>
    public Guid CategoryId { get; } = categoryId;
}

/// <summary>
/// Error indicating a category cannot be deleted because it has a planned amount.
/// </summary>
/// <param name="categoryId">The category ID.</param>
public class CategoryHasPlannedAmountError(Guid categoryId)
    : BudgetError("Budget.CategoryHasPlannedAmount", $"Category '{categoryId}' cannot be deleted because it has a planned amount. Clear the amount first.")
{
    /// <summary>
    /// Gets the category ID.
    /// </summary>
    public Guid CategoryId { get; } = categoryId;
}

/// <summary>
/// Error indicating an invalid planned amount.
/// </summary>
/// <param name="reason">The reason the amount is invalid.</param>
public class InvalidAmountError(string reason)
    : BudgetError("Budget.InvalidAmount", $"Invalid planned amount: {reason}")
{
    /// <summary>
    /// Gets the reason the amount is invalid.
    /// </summary>
    public string Reason { get; } = reason;
}

/// <summary>
/// Error indicating the budget cannot be activated.
/// </summary>
/// <param name="reason">The reason activation failed.</param>
public class ActivationValidationError(string reason)
    : BudgetError("Budget.ActivationFailed", $"Budget cannot be activated: {reason}")
{
    /// <summary>
    /// Gets the reason activation failed.
    /// </summary>
    public string Reason { get; } = reason;
}

/// <summary>
/// Error indicating an operation is not allowed in the current budget status.
/// </summary>
/// <param name="operation">The operation that was attempted.</param>
/// <param name="currentStatus">The current budget status.</param>
public class InvalidStatusTransitionError(string operation, string currentStatus)
    : BudgetError("Budget.InvalidStatusTransition", $"Cannot {operation} when budget is {currentStatus}.")
{
    /// <summary>
    /// Gets the operation that was attempted.
    /// </summary>
    public string Operation { get; } = operation;

    /// <summary>
    /// Gets the current budget status.
    /// </summary>
    public string CurrentStatus { get; } = currentStatus;
}
