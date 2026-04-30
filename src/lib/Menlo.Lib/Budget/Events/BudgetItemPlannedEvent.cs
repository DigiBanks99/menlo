using Menlo.Lib.Budget.ValueObjects;
using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.ValueObjects;

namespace Menlo.Lib.Budget.Events;

/// <summary>
/// Domain event raised when a new budget item is planned (created).
/// </summary>
/// <param name="BudgetId">The budget this item belongs to.</param>
/// <param name="BudgetItemId">The ID of the newly created item.</param>
/// <param name="CategoryId">The category the item belongs to.</param>
/// <param name="Month">The month (1-12) of the item.</param>
/// <param name="PlannedAmount">The planned monetary amount.</param>
public sealed record BudgetItemPlannedEvent(
    BudgetId BudgetId,
    BudgetItemId BudgetItemId,
    BudgetCategoryId CategoryId,
    int Month,
    Money PlannedAmount) : IDomainEvent;
