using Menlo.Lib.Budget.ValueObjects;
using Menlo.Lib.Common.Abstractions;

namespace Menlo.Lib.Budget.Events;

/// <summary>
/// Domain event raised when a budget is activated.
/// </summary>
/// <param name="BudgetId">The ID of the activated budget.</param>
public sealed record BudgetActivatedEvent(BudgetId BudgetId) : IDomainEvent;
