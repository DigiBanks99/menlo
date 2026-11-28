using Menlo.Lib.Budget.ValueObjects;
using Menlo.Lib.Common.Abstractions;

namespace Menlo.Lib.Budget.Events;

/// <summary>
/// Domain event raised when a budget is created for a year.
/// </summary>
/// <param name="BudgetId">The ID of the new budget.</param>
/// <param name="Year">The calendar year the budget covers.</param>
public sealed record BudgetCreatedEvent(BudgetId BudgetId, int Year) : IDomainEvent;
