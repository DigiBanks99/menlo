using Menlo.Lib.Budget.ValueObjects;
using Menlo.Lib.Common.Abstractions;

namespace Menlo.Lib.Budget.Events;

/// <summary>Domain event raised when a category is updated.</summary>
public sealed record CategoryUpdatedEvent(BudgetId BudgetId, BudgetCategoryId CategoryId) : IDomainEvent;
