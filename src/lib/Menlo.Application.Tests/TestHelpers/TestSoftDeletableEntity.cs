using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.Enums;
using Menlo.Lib.Common.ValueObjects;

namespace Menlo.Application.Tests.TestHelpers;

internal sealed class TestSoftDeletableEntity : IAuditable, ISoftDeletable
{
    private TestSoftDeletableEntity(UserId id)
    {
        Id = id;
    }

    public UserId Id { get; }

    // IAuditable
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

    // ISoftDeletable
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public UserId? DeletedBy { get; private set; }

    public void MarkDeleted(UserId deletedBy, DateTimeOffset deletedAt)
    {
        IsDeleted = true;
        DeletedAt = deletedAt;
        DeletedBy = deletedBy;
    }

    public static TestSoftDeletableEntity Create() => new(UserId.NewId());
}
