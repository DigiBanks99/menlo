namespace Menlo.Lib.Budget.ValueObjects;

/// <summary>
/// Represents a strongly-typed identifier for a budget item.
/// </summary>
/// <param name="Value">The underlying unique identifier.</param>
public readonly record struct BudgetItemId(Guid Value)
{
    public static BudgetItemId NewId() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}
