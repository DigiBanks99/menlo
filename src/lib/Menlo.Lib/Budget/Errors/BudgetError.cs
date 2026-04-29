using Menlo.Lib.Common.Abstractions;

namespace Menlo.Lib.Budget.Errors;

/// <summary>
/// Base class for budget domain errors.
/// </summary>
/// <param name="code">Machine-readable error code.</param>
/// <param name="message">Human-readable error message.</param>
public class BudgetError(string code, string message) : Error(code, message);

/// <summary>
/// Error indicating invalid budget data was provided.
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
/// Error indicating a duplicate category name exists within the same parent scope.
/// </summary>
/// <param name="name">The duplicate category name.</param>
public class DuplicateCategoryNameError(string name)
    : BudgetError("Budget.DuplicateCategoryName", $"A category named '{name}' already exists in this scope.")
{
    /// <summary>
    /// Gets the duplicate category name.
    /// </summary>
    public string Name { get; } = name;
}

/// <summary>
/// Error indicating the budget cannot be activated because no category has a non-zero planned amount.
/// </summary>
public class BudgetActivationError(string reason)
    : BudgetError("Budget.ActivationFailed", $"Budget cannot be activated: {reason}")
{
    /// <summary>
    /// Gets the reason activation failed.
    /// </summary>
    public string Reason { get; } = reason;
}

/// <summary>
/// Error indicating an operation was attempted on a budget in an invalid status.
/// </summary>
/// <param name="operation">The operation attempted.</param>
/// <param name="currentStatus">The current budget status.</param>
public class InvalidBudgetStatusError(string operation, string currentStatus)
    : BudgetError("Budget.InvalidStatus", $"Cannot perform '{operation}' on a budget with status '{currentStatus}'.")
{
    /// <summary>
    /// Gets the operation that was attempted.
    /// </summary>
    public string Operation { get; } = operation;

    /// <summary>
    /// Gets the current status of the budget.
    /// </summary>
    public string CurrentStatus { get; } = currentStatus;
}

/// <summary>
/// Error indicating a category was not found in the budget.
/// </summary>
/// <param name="categoryId">The category ID that was not found.</param>
public class CategoryNotFoundError(string categoryId)
    : BudgetError("Budget.CategoryNotFound", $"Category '{categoryId}' not found in this budget.")
{
    /// <summary>Gets the missing category ID.</summary>
    public string CategoryId { get; } = categoryId;
}

/// <summary>
/// Error indicating a depth violation in the category hierarchy.
/// </summary>
/// <param name="reason">The reason for the depth violation.</param>
public class CategoryDepthError(string reason)
    : BudgetError("Budget.CategoryDepthViolation", $"Category depth violation: {reason}")
{
    /// <summary>Gets the reason for the depth violation.</summary>
    public string Reason { get; } = reason;
}

/// <summary>
/// Error indicating the category's parent is deleted and cannot accept children.
/// </summary>
public class DeletedParentError()
    : BudgetError("Budget.DeletedParent", "Cannot add a child to a soft-deleted parent category.");
