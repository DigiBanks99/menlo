namespace Menlo.Lib.Common.Abstractions;

/// <summary>
/// Represents a domain entity with a strongly-typed identity.
/// Entities have a unique identifier that distinguishes them from other entities.
/// </summary>
/// <typeparam name="TId">The type of the entity's identifier. Must be a strongly-typed ID (e.g., readonly record struct).</typeparam>
public interface IEntity<out TId>
{
    /// <summary>
    /// Gets the unique identifier for this entity.
    /// </summary>
    TId Id { get; }
}
