using Menlo.Lib.Auth.ValueObjects;
using Shouldly;

namespace Menlo.Lib.Tests.Auth.ValueObjects;

/// <summary>
/// Tests for the ExternalUserId value object.
/// </summary>
public sealed class ExternalUserIdTests
{
    [Fact]
    public void GivenExternalUserId_WhenCallingToString()
    {
        // Arrange
        const string value = "auth0|abc123";
        ExternalUserId id = new(value);

        // Act
        string result = id.ToString();

        // Assert
        ItShouldReturnValue(result, value);
    }

    private static void ItShouldReturnValue(string result, string expected)
    {
        result.ShouldBe(expected);
    }

    [Fact]
    public void GivenTwoExternalUserIdsWithSameValue_WhenComparing()
    {
        // Arrange
        const string value = "auth0|abc123";

        // Act
        ExternalUserId id1 = new(value);
        ExternalUserId id2 = new(value);

        // Assert
        ItShouldBeEqual(id1, id2);
    }

    private static void ItShouldBeEqual(ExternalUserId id1, ExternalUserId id2)
    {
        id1.ShouldBe(id2);
    }

    [Fact]
    public void GivenTwoExternalUserIdsWithDifferentValues_WhenComparing()
    {
        // Arrange
        ExternalUserId id1 = new("auth0|abc123");
        ExternalUserId id2 = new("auth0|xyz789");

        // Assert
        ItShouldNotBeEqual(id1, id2);
    }

    private static void ItShouldNotBeEqual(ExternalUserId id1, ExternalUserId id2)
    {
        id1.ShouldNotBe(id2);
    }
}


