using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.ValueObjects;

namespace Menlo.Application.Common.Stamps;

/// <summary>Stamps every soft delete with the <see cref="UserId"/> it was given.</summary>
public sealed class SoftDeleteStampFactory(UserId actorId) : ISoftDeleteStampFactory
{
    public SoftDeleteStamp CreateStamp() => new(actorId, DateTimeOffset.UtcNow);
}
