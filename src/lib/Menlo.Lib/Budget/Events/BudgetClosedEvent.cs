using Menlo.Lib.Budget.ValueObjects;
using Menlo.Lib.Common.Abstractions;

namespace Menlo.Lib.Budget.Events;

/// <summary>
/// Domain event raised when a budget is closed.
/// </summary>
/// <param name="BudgetId">The ID of the closed budget.</param>
public sealed record BudgetClosedEvent(BudgetId BudgetId) : IDomainEvent;
