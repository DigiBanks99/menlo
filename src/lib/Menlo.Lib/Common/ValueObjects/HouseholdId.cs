namespace Menlo.Lib.Common.ValueObjects;

/// <summary>
/// Represents a strongly-typed identifier for a household in the system.
/// </summary>
/// <param name="Value">The underlying unique identifier.</param>
public readonly record struct HouseholdId(Guid Value)
{
    /// <summary>
    /// Creates a new HouseholdId with a new unique value.
    /// </summary>
    public static HouseholdId NewId() => new(Guid.NewGuid());

    /// <summary>
    /// Returns the string representation of the household ID.
    /// </summary>
    public override string ToString() => Value.ToString();
}
