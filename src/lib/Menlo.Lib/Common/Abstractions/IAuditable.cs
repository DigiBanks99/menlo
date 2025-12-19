using Menlo.Lib.Common.Enums;
using Menlo.Lib.Common.ValueObjects;

namespace Menlo.Lib.Common.Abstractions;

/// <summary>
/// Interface for entities that track audit information (who created/modified and when).
/// Audit fields are read-only from outside the entity. The Audit method is the
/// only way to mutate audit fields, ensuring consistency.
/// </summary>
public interface IAuditable
{
    /// <summary>
    /// Gets the user who created this entity.
    /// Null until the entity has been audited with AuditOperation.Create.
    /// </summary>
    UserId? CreatedBy { get; }

    /// <summary>
    /// Gets when this entity was created (UTC).
    /// Null until the entity has been audited with AuditOperation.Create.
    /// </summary>
    DateTimeOffset? CreatedAt { get; }

    /// <summary>
    /// Gets the user who last modified this entity.
    /// Updated on both Create and Update operations.
    /// </summary>
    UserId? ModifiedBy { get; }

    /// <summary>
    /// Gets when this entity was last modified (UTC).
    /// Updated on both Create and Update operations.
    /// </summary>
    DateTimeOffset? ModifiedAt { get; }

    /// <summary>
    /// Performs an audit operation, updating the appropriate audit fields.
    /// For Create operations, sets both Created and Modified fields.
    /// For Update operations, sets only Modified fields.
    /// </summary>
    /// <param name="factory">Factory to create the current audit stamp. Must not be null.</param>
    /// <param name="operation">The type of audit operation being performed.</param>
    void Audit(IAuditStampFactory factory, AuditOperation operation);
}
