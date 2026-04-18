using Menlo.Lib.Budget.ValueObjects;
using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.ValueObjects;

namespace Menlo.Lib.Budget.Events;

/// <summary>
/// Domain event raised when the planned monthly amount for a budget category is set.
/// </summary>
/// <param name="BudgetId">The ID of the budget containing the category.</param>
/// <param name="CategoryId">The ID of the category whose planned amount was set.</param>
/// <param name="Amount">The new planned monthly amount.</param>
public sealed record PlannedAmountSetEvent(
    BudgetId BudgetId,
    BudgetCategoryId CategoryId,
    Money Amount) : IDomainEvent;
