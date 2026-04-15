using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.Enums;
using Menlo.Lib.Common.ValueObjects;

namespace Menlo.Application.Tests.Fixtures;

internal sealed class TestAuditableEntity : IAuditable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public UserId? CreatedBy { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public UserId? ModifiedBy { get; set; }
    public DateTimeOffset? ModifiedAt { get; set; }

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
