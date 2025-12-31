using Menlo.Lib.Auth.Events;
using Menlo.Lib.Common.ValueObjects;
using Shouldly;

namespace Menlo.Lib.Tests.Auth.Events;

/// <summary>
/// Tests for UserLoggedInEvent domain event.
/// </summary>
public sealed class UserLoggedInEventTests
{
    [Fact]
    public void GivenUserIdAndTimestamp_WhenCreatingEvent()
    {
        UserId userId = UserId.NewId();
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;

        UserLoggedInEvent loginEvent = new(userId, timestamp);

        ItShouldHaveUserId(loginEvent, userId);
        ItShouldHaveTimestamp(loginEvent, timestamp);
    }

    [Fact]
    public void GivenTwoEventsWithSameData_WhenComparingEvents()
    {
        UserId userId = UserId.NewId();
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;
        UserLoggedInEvent event1 = new(userId, timestamp);
        UserLoggedInEvent event2 = new(userId, timestamp);

        ItShouldBeEqual(event1, event2);
    }

    [Fact]
    public void GivenTwoEventsWithDifferentUserIds_WhenComparingEvents()
    {
        UserId userId1 = UserId.NewId();
        UserId userId2 = UserId.NewId();
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;
        UserLoggedInEvent event1 = new(userId1, timestamp);
        UserLoggedInEvent event2 = new(userId2, timestamp);

        ItShouldNotBeEqual(event1, event2);
    }

    [Fact]
    public void GivenTwoEventsWithDifferentTimestamps_WhenComparingEvents()
    {
        UserId userId = UserId.NewId();
        DateTimeOffset timestamp1 = DateTimeOffset.UtcNow;
        DateTimeOffset timestamp2 = DateTimeOffset.UtcNow.AddMinutes(5);
        UserLoggedInEvent event1 = new(userId, timestamp1);
        UserLoggedInEvent event2 = new(userId, timestamp2);

        ItShouldNotBeEqual(event1, event2);
    }

    // Assertion Helpers
    private static void ItShouldHaveUserId(UserLoggedInEvent loginEvent, UserId expectedUserId)
    {
        loginEvent.UserId.ShouldBe(expectedUserId);
    }

    private static void ItShouldHaveTimestamp(UserLoggedInEvent loginEvent, DateTimeOffset expectedTimestamp)
    {
        loginEvent.Timestamp.ShouldBe(expectedTimestamp);
    }

    private static void ItShouldBeEqual(UserLoggedInEvent event1, UserLoggedInEvent event2)
    {
        event1.ShouldBe(event2);
        event1.GetHashCode().ShouldBe(event2.GetHashCode());
    }

    private static void ItShouldNotBeEqual(UserLoggedInEvent event1, UserLoggedInEvent event2)
    {
        event1.ShouldNotBe(event2);
    }
}
