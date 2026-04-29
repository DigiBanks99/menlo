using Menlo.Lib.Budget.ValueObjects;
using Menlo.Lib.Common.Abstractions;

namespace Menlo.Lib.Budget.Events;

/// <summary>Domain event raised when a category is reparented.</summary>
public sealed record CategoryReparentedEvent(
    BudgetId BudgetId,
    BudgetCategoryId CategoryId,
    BudgetCategoryId? OldParentId,
    BudgetCategoryId? NewParentId) : IDomainEvent;
