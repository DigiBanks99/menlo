using Menlo.Application.Common.Stamps;
using Menlo.Lib.Common;
using Menlo.Lib.Common.ValueObjects;
using Microsoft.Extensions.Time.Testing;

namespace Menlo.Application.Tests.Common.Stamps;

public sealed class StampFactoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 15, 8, 30, 0, TimeSpan.Zero);

    private readonly FakeTimeProvider _time = new(Now);

    [Test]
    public void AuditStampFactory_StampsTheUserAndTimeItWasGiven()
    {
        UserId actor = UserId.NewId();
        AuditStampFactory sut = new(actor, _time);

        AuditStamp stamp = sut.CreateStamp();

        stamp.ActorId.ShouldBe(actor);
        stamp.CorrelationId.ShouldBeNull();
        stamp.Timestamp.ShouldBe(Now);
    }

    [Test]
    public void AuditStampFactory_FollowsTheClock()
    {
        AuditStampFactory sut = new(SystemUser.Id, _time);
        _time.Advance(TimeSpan.FromHours(2));

        sut.CreateStamp().Timestamp.ShouldBe(Now.AddHours(2));
    }

    [Test]
    public void AuditStampFactory_CarriesTheCorrelationIdItWasGiven()
    {
        AuditStampFactory sut = new(SystemUser.Id, _time, "budget-seed");

        AuditStamp stamp = sut.CreateStamp();

        stamp.ActorId.ShouldBe(SystemUser.Id);
        stamp.CorrelationId.ShouldBe("budget-seed");
    }

    [Test]
    public void SoftDeleteStampFactory_StampsTheUserAndTimeItWasGiven()
    {
        UserId actor = UserId.NewId();
        SoftDeleteStampFactory sut = new(actor, _time);

        SoftDeleteStamp stamp = sut.CreateStamp();

        stamp.ActorId.ShouldBe(actor);
        stamp.Timestamp.ShouldBe(Now);
    }

    [Test]
    public void TheSystemUser_IsOneFixedId()
    {
        SystemUser.Id.Value.ShouldBe(Guid.Parse("00000000-0000-0000-0000-000000000001"));
    }
}
