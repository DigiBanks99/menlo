using Menlo.Lib.Budget.ValueObjects;
using Menlo.Lib.Common.Abstractions;

namespace Menlo.Lib.Budget.Events;

/// <summary>
/// Domain event raised when a category is added to a budget.
/// </summary>
/// <param name="BudgetId">The ID of the budget the category belongs to.</param>
/// <param name="CategoryId">The ID of the new category.</param>
/// <param name="Name">The category's name.</param>
/// <param name="ParentId">The ID of the category it sits under, or <see langword="null"/> for a top-level category.</param>
public sealed record CategoryAddedEvent(BudgetId BudgetId, CategoryId CategoryId, string Name, CategoryId? ParentId) : IDomainEvent;
