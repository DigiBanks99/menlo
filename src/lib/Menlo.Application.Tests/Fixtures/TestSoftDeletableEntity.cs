using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.Enums;
using Menlo.Lib.Common.ValueObjects;

namespace Menlo.Application.Tests.Fixtures;

internal sealed class TestSoftDeletableEntity : IAuditable, ISoftDeletable
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Name { get; init; }

    // IAuditable
    public UserId? CreatedBy { get; set; }
    public DateTimeOffset? CreatedAt { get; private set; }
    public UserId? ModifiedBy { get; private set; }
    public DateTimeOffset? ModifiedAt { get; private set; }

    // ISoftDeletable
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public UserId? DeletedBy { get; private set; }

    public void Delete(ISoftDeleteStampFactory factory)
    {
        SoftDeleteStamp stamp = factory.CreateStamp();
        IsDeleted = true;
        DeletedBy = stamp.ActorId;
    }

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
}
