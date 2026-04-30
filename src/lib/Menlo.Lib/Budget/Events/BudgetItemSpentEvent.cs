using Menlo.Lib.Budget.ValueObjects;
using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.ValueObjects;

namespace Menlo.Lib.Budget.Events;

/// <summary>
/// Domain event raised when a budget item payment is recorded.
/// </summary>
public sealed record BudgetItemSpentEvent(
    BudgetId BudgetId,
    BudgetItemId BudgetItemId,
    Money SpentAmount) : IDomainEvent;
