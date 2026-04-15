using Menlo.Lib.Common.ValueObjects;
using Shouldly;

namespace Menlo.Lib.Tests.Common.ValueObjects;

/// <summary>
/// Tests for the SoftDeleteStamp value object.
/// </summary>
public sealed class SoftDeleteStampTests
{
    [Fact]
    public void GivenActorIdAndTimestamp_WhenCreatingSoftDeleteStamp()
    {
        // Arrange
        UserId actorId = UserId.NewId();
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;

        // Act
        SoftDeleteStamp stamp = new(actorId, timestamp);

        // Assert
        ItShouldStoreCorrectValues(stamp, actorId, timestamp);
    }

    private static void ItShouldStoreCorrectValues(SoftDeleteStamp stamp, UserId expectedActorId, DateTimeOffset expectedTimestamp)
    {
        stamp.ActorId.ShouldBe(expectedActorId);
        stamp.Timestamp.ShouldBe(expectedTimestamp);
    }

    [Fact]
    public void GivenTwoStampsWithSameValues_WhenComparing()
    {
        // Arrange
        UserId actorId = UserId.NewId();
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;

        // Act
        SoftDeleteStamp stamp1 = new(actorId, timestamp);
        SoftDeleteStamp stamp2 = new(actorId, timestamp);

        // Assert
        ItShouldBeEqual(stamp1, stamp2);
    }

    private static void ItShouldBeEqual(SoftDeleteStamp stamp1, SoftDeleteStamp stamp2)
    {
        stamp1.ShouldBe(stamp2);
    }

    [Fact]
    public void GivenTwoStampsWithDifferentValues_WhenComparing()
    {
        // Arrange
        UserId actorId1 = UserId.NewId();
        UserId actorId2 = UserId.NewId();
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;

        // Act
        SoftDeleteStamp stamp1 = new(actorId1, timestamp);
        SoftDeleteStamp stamp2 = new(actorId2, timestamp);

        // Assert
        ItShouldNotBeEqual(stamp1, stamp2);
    }

    private static void ItShouldNotBeEqual(SoftDeleteStamp stamp1, SoftDeleteStamp stamp2)
    {
        stamp1.ShouldNotBe(stamp2);
    }

    [Fact]
    public void GivenSoftDeleteStamp_WhenDeconstructing()
    {
        // Arrange
        UserId actorId = UserId.NewId();
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;
        SoftDeleteStamp stamp = new(actorId, timestamp);

        // Act
        (UserId deconstructedActorId, DateTimeOffset deconstructedTimestamp) = stamp;

        // Assert
        ItShouldDeconstructCorrectly(deconstructedActorId, deconstructedTimestamp, actorId, timestamp);
    }

    private static void ItShouldDeconstructCorrectly(UserId deconstructedActorId, DateTimeOffset deconstructedTimestamp, UserId expectedActorId, DateTimeOffset expectedTimestamp)
    {
        deconstructedActorId.ShouldBe(expectedActorId);
        deconstructedTimestamp.ShouldBe(expectedTimestamp);
    }
}
