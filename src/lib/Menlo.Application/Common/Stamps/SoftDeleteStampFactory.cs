using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.ValueObjects;

namespace Menlo.Application.Common.Stamps;

/// <summary>Stamps every soft delete with the <see cref="UserId"/> it was given, at the time the <see cref="TimeProvider"/> reports.</summary>
public sealed class SoftDeleteStampFactory(UserId actorId, TimeProvider timeProvider) : ISoftDeleteStampFactory
{
    public SoftDeleteStamp CreateStamp() => new(actorId, timeProvider.GetUtcNow());
}
