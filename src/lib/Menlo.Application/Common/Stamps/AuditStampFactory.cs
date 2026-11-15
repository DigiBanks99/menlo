using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.ValueObjects;

namespace Menlo.Application.Common.Stamps;

/// <summary>Stamps every audit with the <see cref="UserId"/> it was given, at the time the <see cref="TimeProvider"/> reports.</summary>
public sealed class AuditStampFactory(UserId actorId, TimeProvider timeProvider, string? correlationId = null) : IAuditStampFactory
{
    public AuditStamp CreateStamp() => new(actorId, timeProvider.GetUtcNow(), correlationId);
}
