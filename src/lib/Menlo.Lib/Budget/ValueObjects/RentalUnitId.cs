namespace Menlo.Lib.Budget.ValueObjects;

/// <summary>
/// Represents a strongly-typed identifier for a rental unit.
/// </summary>
/// <param name="Value">The underlying unique identifier.</param>
public readonly record struct RentalUnitId(Guid Value)
{
    /// <summary>
    /// Creates a new RentalUnitId with a new unique value.
    /// </summary>
    public static RentalUnitId NewId() => new(Guid.NewGuid());

    /// <summary>
    /// Returns the string representation of the rental unit ID.
    /// </summary>
    public override string ToString() => Value.ToString();
}
