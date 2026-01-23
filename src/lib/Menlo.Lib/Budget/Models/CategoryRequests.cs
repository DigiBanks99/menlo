namespace Menlo.Lib.Budget.Models;

/// <summary>
/// Request to create a new budget category.
/// </summary>
/// <param name="Name">Category name (required, unique among siblings)</param>
/// <param name="Description">Optional description</param>
/// <param name="ParentId">Parent category ID for subcategories (null for root categories)</param>
public record CreateCategoryRequest(
    string Name,
    string? Description = null,
    Guid? ParentId = null);

/// <summary>
/// Request to update an existing budget category.
/// </summary>
/// <param name="Name">Updated category name (required, unique among siblings)</param>
/// <param name="Description">Updated description</param>
public record UpdateCategoryRequest(
    string Name,
    string? Description = null);

/// <summary>
/// Request to set planned amount for a category.
/// </summary>
/// <param name="Amount">The amount</param>
/// <param name="Currency">The currency code (must match budget currency)</param>
public record SetPlannedAmountRequest(
    decimal Amount,
    string Currency);

/// <summary>
/// Request to reorder a category.
/// </summary>
/// <param name="DisplayOrder">New display order position</param>
public record ReorderCategoryRequest(
    int DisplayOrder);