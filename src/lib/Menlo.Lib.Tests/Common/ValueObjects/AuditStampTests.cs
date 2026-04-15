using Menlo.Lib.Common.ValueObjects;
using Shouldly;

namespace Menlo.Lib.Tests.Common.ValueObjects;

/// <summary>
/// Tests for the AuditStamp ToString method.
/// </summary>
public sealed class AuditStampTests
{
    [Fact]
    public void GivenAuditStampWithoutCorrelationId_WhenCallingToString()
    {
        // Arrange
        UserId actorId = UserId.NewId();
        DateTimeOffset timestamp = new(2024, 6, 15, 10, 30, 0, TimeSpan.Zero);
        AuditStamp stamp = new(actorId, timestamp);

        // Act
        string result = stamp.ToString();

        // Assert
        ItShouldReturnStringWithoutCorrelationId(result, actorId, timestamp);
    }

    private static void ItShouldReturnStringWithoutCorrelationId(string result, UserId actorId, DateTimeOffset timestamp)
    {
        result.ShouldBe($"By {actorId} at {timestamp:yyyy-MM-dd HH:mm:ss}Z");
    }

    [Fact]
    public void GivenAuditStampWithCorrelationId_WhenCallingToString()
    {
        // Arrange
        UserId actorId = UserId.NewId();
        DateTimeOffset timestamp = new(2024, 6, 15, 10, 30, 0, TimeSpan.Zero);
        const string correlationId = "abc-123";
        AuditStamp stamp = new(actorId, timestamp, correlationId);

        // Act
        string result = stamp.ToString();

        // Assert
        ItShouldReturnStringWithCorrelationId(result, actorId, timestamp, correlationId);
    }

    private static void ItShouldReturnStringWithCorrelationId(string result, UserId actorId, DateTimeOffset timestamp, string correlationId)
    {
        result.ShouldBe($"By {actorId} at {timestamp:yyyy-MM-dd HH:mm:ss}Z [{correlationId}]");
    }
}


