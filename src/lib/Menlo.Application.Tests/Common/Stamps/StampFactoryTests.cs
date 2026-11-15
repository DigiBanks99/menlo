using Menlo.Application.Common.Stamps;
using Menlo.Lib.Common;
using Menlo.Lib.Common.ValueObjects;

namespace Menlo.Application.Tests.Common.Stamps;

public sealed class StampFactoryTests
{
    [Test]
    public void AuditStampFactory_StampsTheUserItWasGiven()
    {
        UserId actor = UserId.NewId();
        AuditStampFactory sut = new(actor);

        AuditStamp stamp = sut.CreateStamp();

        stamp.ActorId.ShouldBe(actor);
        stamp.CorrelationId.ShouldBeNull();
        stamp.Timestamp.ShouldBeGreaterThan(DateTimeOffset.UtcNow.AddMinutes(-1));
    }

    [Test]
    public void AuditStampFactory_CarriesTheCorrelationIdItWasGiven()
    {
        AuditStampFactory sut = new(SystemUser.Id, "budget-seed");

        AuditStamp stamp = sut.CreateStamp();

        stamp.ActorId.ShouldBe(SystemUser.Id);
        stamp.CorrelationId.ShouldBe("budget-seed");
    }

    [Test]
    public void SoftDeleteStampFactory_StampsTheUserItWasGiven()
    {
        UserId actor = UserId.NewId();
        SoftDeleteStampFactory sut = new(actor);

        SoftDeleteStamp stamp = sut.CreateStamp();

        stamp.ActorId.ShouldBe(actor);
        stamp.Timestamp.ShouldBeGreaterThan(DateTimeOffset.UtcNow.AddMinutes(-1));
    }

    [Test]
    public void TheSystemUser_IsOneFixedId()
    {
        SystemUser.Id.Value.ShouldBe(Guid.Parse("00000000-0000-0000-0000-000000000001"));
    }
}
