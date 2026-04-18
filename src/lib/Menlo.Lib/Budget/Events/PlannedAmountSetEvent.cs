using Menlo.Lib.Budget.ValueObjects;
using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.ValueObjects;

namespace Menlo.Lib.Budget.Events;

/// <summary>
/// Domain event raised when a planned monthly amount is set for a budget category.
/// </summary>
/// <param name="BudgetId">The ID of the budget.</param>
/// <param name="CategoryId">The ID of the category whose planned amount was updated.</param>
/// <param name="Amount">The new planned monthly amount.</param>
public sealed record PlannedAmountSetEvent(BudgetId BudgetId, BudgetCategoryId CategoryId, Money Amount) : IDomainEvent;
