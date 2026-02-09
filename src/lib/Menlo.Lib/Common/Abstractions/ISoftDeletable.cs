namespace Menlo.Lib.Common.Abstractions;

/// <summary>
/// Interface for entities that support soft deletion.
/// Soft deleted entities are not physically removed from the database but are marked as deleted.
/// Global query filters automatically exclude soft-deleted records from normal queries.
/// </summary>
public interface ISoftDeletable
{
    /// <summary>
    /// Gets whether this entity has been soft deleted.
    /// </summary>
    bool IsDeleted { get; }

    /// <summary>
    /// Gets when this entity was soft deleted (UTC).
    /// Null if the entity has not been deleted.
    /// </summary>
    DateTimeOffset? DeletedAt { get; }

    /// <summary>
    /// Gets the user who deleted this entity.
    /// Null if the entity has not been deleted.
    /// </summary>
    Common.ValueObjects.UserId? DeletedBy { get; }

    /// <summary>
    /// Marks this entity as soft deleted.
    /// </summary>
    /// <param name="factory">Factory to create the current audit stamp containing user and timestamp.</param>
    void SoftDelete(IAuditStampFactory factory);

    /// <summary>
    /// Restores a soft deleted entity, clearing the deletion markers.
    /// </summary>
    void Restore();
}
