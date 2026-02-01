namespace Menlo.Lib.Budget.ValueObjects;

/// <summary>
/// Represents a strongly-typed identifier for a budget category.
/// </summary>
/// <param name="Value">The underlying unique identifier.</param>
public readonly record struct BudgetCategoryId(Guid Value)
{
    /// <summary>
    /// Creates a new BudgetCategoryId with a new unique value.
    /// </summary>
    public static BudgetCategoryId NewId() => new(Guid.NewGuid());

    /// <summary>
    /// Returns the string representation of the category ID.
    /// </summary>
    public override string ToString() => Value.ToString();
}
