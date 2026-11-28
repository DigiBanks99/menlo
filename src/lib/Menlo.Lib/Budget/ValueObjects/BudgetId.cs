namespace Menlo.Lib.Budget.ValueObjects;

/// <summary>
/// Represents a strongly-typed identifier for a budget.
/// </summary>
/// <param name="Value">The underlying unique identifier.</param>
public readonly record struct BudgetId(Guid Value)
{
    /// <summary>
    /// Creates a new BudgetId with a new unique value.
    /// </summary>
    public static BudgetId NewId() => new(Guid.NewGuid());

    /// <summary>
    /// Returns the string representation of the budget ID.
    /// </summary>
    public override string ToString() => Value.ToString();
}
