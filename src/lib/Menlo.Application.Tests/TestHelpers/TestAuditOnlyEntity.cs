using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.Enums;
using Menlo.Lib.Common.ValueObjects;

namespace Menlo.Application.Tests.TestHelpers;

/// <summary>
/// Test entity that is <see cref="IAuditable"/> but NOT <see cref="ISoftDeletable"/>.
/// Used to verify the <c>AuditingInterceptor</c> correctly skips audit stamping for
/// entities in the <see cref="Microsoft.EntityFrameworkCore.EntityState.Deleted"/> state.
/// </summary>
internal sealed class TestAuditOnlyEntity : IAuditable
{
    private TestAuditOnlyEntity(UserId id) => Id = id;

    public UserId Id { get; }
    public UserId? CreatedBy { get; private set; }
    public DateTimeOffset? CreatedAt { get; private set; }
    public UserId? ModifiedBy { get; private set; }
    public DateTimeOffset? ModifiedAt { get; private set; }

    public void Audit(IAuditStampFactory factory, AuditOperation operation)
    {
        AuditStamp stamp = factory.CreateStamp();
        if (operation == AuditOperation.Create)
        {
            CreatedBy = stamp.ActorId;
            CreatedAt = stamp.Timestamp;
        }
        ModifiedBy = stamp.ActorId;
        ModifiedAt = stamp.Timestamp;
    }

    public static TestAuditOnlyEntity Create() => new(UserId.NewId());
}
