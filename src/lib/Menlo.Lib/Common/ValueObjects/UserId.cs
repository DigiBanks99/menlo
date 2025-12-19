namespace Menlo.Lib.Common.ValueObjects;

/// <summary>
/// Represents a strongly-typed identifier for a user (person) in the system.
/// Used for auditing, ownership, and authorization purposes across all domains.
/// </summary>
/// <param name="Value">The underlying unique identifier.</param>
public readonly record struct UserId(Guid Value)
{
    /// <summary>
    /// Creates a new UserId with a new unique value.
    /// </summary>
    public static UserId NewId() => new(Guid.NewGuid());

    /// <summary>
    /// Returns the string representation of the user ID.
    /// </summary>
    public override string ToString() => Value.ToString();
}
