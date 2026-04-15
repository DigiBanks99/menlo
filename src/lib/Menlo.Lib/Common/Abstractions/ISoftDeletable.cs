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
    bool IsDeleted { get; set; }

    /// <summary>When this entity was soft-deleted (UTC). Null if not deleted.</summary>
    DateTimeOffset? DeletedAt { get; set; }

    /// <summary>The user who soft-deleted this entity. Null if not deleted.</summary>
    UserId? DeletedBy { get; set; }
}


