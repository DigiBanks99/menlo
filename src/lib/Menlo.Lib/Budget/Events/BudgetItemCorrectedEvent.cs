using Menlo.Lib.Budget.ValueObjects;
using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.ValueObjects;

namespace Menlo.Lib.Budget.Events;

/// <summary>
/// Domain event raised when a budget item amount is corrected.
/// </summary>
/// <param name="BudgetId">The budget this item belongs to.</param>
/// <param name="BudgetItemId">The corrected item's ID.</param>
/// <param name="Field">Which field was corrected (PlannedAmount, RealizedAmount, SpentAmount).</param>
/// <param name="OldAmount">The previous amount.</param>
/// <param name="NewAmount">The new amount.</param>
public sealed record BudgetItemCorrectedEvent(
    BudgetId BudgetId,
    BudgetItemId BudgetItemId,
    string Field,
    Money OldAmount,
    Money NewAmount) : IDomainEvent;
