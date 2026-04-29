namespace Menlo.Lib.Budget.ValueObjects;

/// <summary>
/// Represents a strongly-typed identifier for a canonical category, providing cross-year identity.
/// </summary>
/// <param name="Value">The underlying unique identifier.</param>
public readonly record struct CanonicalCategoryId(Guid Value)
{
    /// <summary>
    /// Creates a new CanonicalCategoryId with a new unique value.
    /// </summary>
    public static CanonicalCategoryId NewId() => new(Guid.NewGuid());

    /// <summary>
    /// Returns the string representation of the canonical category ID.
    /// </summary>
    public override string ToString() => Value.ToString();
}
