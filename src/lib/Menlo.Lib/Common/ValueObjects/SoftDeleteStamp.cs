namespace Menlo.Lib.Common.ValueObjects;

/// <summary>
/// Captures who performed a soft-delete and when.
/// </summary>
/// <param name="ActorId">The user who deleted the entity.</param>
/// <param name="Timestamp">When the deletion occurred (UTC).</param>
public readonly record struct SoftDeleteStamp(UserId ActorId, DateTimeOffset Timestamp);
