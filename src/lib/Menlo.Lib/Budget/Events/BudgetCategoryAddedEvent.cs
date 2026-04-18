using Menlo.Lib.Budget.ValueObjects;
using Menlo.Lib.Common.Abstractions;

namespace Menlo.Lib.Budget.Events;

/// <summary>
/// Domain event raised when a category is added to a budget.
/// </summary>
/// <param name="BudgetId">The ID of the budget the category was added to.</param>
/// <param name="CategoryId">The ID of the newly added category.</param>
/// <param name="Name">The name of the newly added category.</param>
/// <param name="ParentId">The parent category ID, if any.</param>
public sealed record BudgetCategoryAddedEvent(
    BudgetId BudgetId,
    BudgetCategoryId CategoryId,
    string Name,
    BudgetCategoryId? ParentId) : IDomainEvent;
