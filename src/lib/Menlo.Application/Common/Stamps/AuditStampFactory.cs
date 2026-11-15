using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.ValueObjects;

namespace Menlo.Application.Common.Stamps;

/// <summary>Stamps every audit with the <see cref="UserId"/> it was given.</summary>
public sealed class AuditStampFactory(UserId actorId, string? correlationId = null) : IAuditStampFactory
{
    public AuditStamp CreateStamp() => new(actorId, DateTimeOffset.UtcNow, correlationId);
}
