namespace Menlo.Lib.Budget.ValueObjects;

/// <summary>
/// Represents a strongly-typed identifier for a budget's category.
/// </summary>
/// <param name="Value">The underlying unique identifier.</param>
public readonly record struct CategoryId(Guid Value)
{
    /// <summary>
    /// Creates a new CategoryId with a new unique value.
    /// </summary>
    public static CategoryId NewId() => new(Guid.NewGuid());

    /// <summary>
    /// Returns the string representation of the category ID.
    /// </summary>
    public override string ToString() => Value.ToString();
}
