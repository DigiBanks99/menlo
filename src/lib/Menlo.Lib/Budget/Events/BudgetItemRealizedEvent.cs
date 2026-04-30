using Menlo.Lib.Budget.ValueObjects;
using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.ValueObjects;

namespace Menlo.Lib.Budget.Events;

/// <summary>
/// Domain event raised when a budget item is realized (bill/payslip arrives).
/// </summary>
public sealed record BudgetItemRealizedEvent(
    BudgetId BudgetId,
    BudgetItemId BudgetItemId,
    Money RealizedAmount) : IDomainEvent;
