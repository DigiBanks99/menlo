namespace Menlo.Lib.Common.ValueObjects;

/// <summary>
/// Represents a point-in-time snapshot of who performed an action and when.
/// Used by IAuditable entities to track creation and modification.
/// </summary>
/// <param name="ActorId">The user who performed the action.</param>
/// <param name="Timestamp">When the action was performed (UTC).</param>
/// <param name="CorrelationId">Optional correlation ID for distributed tracing.</param>
public readonly record struct AuditStamp(
    UserId ActorId,
    DateTimeOffset Timestamp,
    string? CorrelationId = null)
{
    /// <summary>
    /// Returns a string representation of this audit stamp.
    /// </summary>
    public override string ToString() =>
        $"By {ActorId} at {Timestamp:yyyy-MM-dd HH:mm:ss}Z" +
        (CorrelationId != null ? $" [{CorrelationId}]" : string.Empty);
}
