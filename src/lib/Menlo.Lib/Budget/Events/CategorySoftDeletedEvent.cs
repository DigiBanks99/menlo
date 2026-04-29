using Menlo.Lib.Budget.ValueObjects;
using Menlo.Lib.Common.Abstractions;

namespace Menlo.Lib.Budget.Events;

/// <summary>Domain event raised when a category (and its children) are soft-deleted.</summary>
public sealed record CategorySoftDeletedEvent(BudgetId BudgetId, BudgetCategoryId CategoryId) : IDomainEvent;
