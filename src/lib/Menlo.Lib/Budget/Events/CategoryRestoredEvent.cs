using Menlo.Lib.Budget.ValueObjects;
using Menlo.Lib.Common.Abstractions;

namespace Menlo.Lib.Budget.Events;

/// <summary>Domain event raised when a category (and its children) are restored.</summary>
public sealed record CategoryRestoredEvent(BudgetId BudgetId, BudgetCategoryId CategoryId) : IDomainEvent;
