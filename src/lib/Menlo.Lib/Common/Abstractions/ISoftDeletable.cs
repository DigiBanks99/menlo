using Menlo.Lib.Common.ValueObjects;

namespace Menlo.Lib.Common.Abstractions;

/// <summary>
/// Interface for entities that support soft deletion.
/// Soft-deleted records are excluded from standard queries via a global EF Core query filter.
/// Use <see cref="MarkDeleted"/> to soft-delete; never set properties directly.
/// </summary>
public interface ISoftDeletable
{
    /// <summary>Whether this entity has been soft-deleted.</summary>
    bool IsDeleted { get; }

    /// <summary>When this entity was soft-deleted (UTC). Null if not deleted.</summary>
    DateTimeOffset? DeletedAt { get; }

    /// <summary>The user who soft-deleted this entity. Null if not deleted.</summary>
    UserId? DeletedBy { get; }

    /// <summary>
    /// Marks this entity as soft-deleted with the information provided by the factory.
    /// </summary>
    /// <param name="factory">The factory to create a soft delete stamp.</param>
    void Delete(ISoftDeleteStampFactory factory);
}


