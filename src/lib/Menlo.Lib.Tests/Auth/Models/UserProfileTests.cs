using Menlo.Lib.Auth.Models;
using Shouldly;

namespace Menlo.Lib.Tests.Auth.Models;

/// <summary>
/// Tests for the UserProfile record.
/// </summary>
public sealed class UserProfileTests
{
    [Fact]
    public void GivenValidData_WhenCreatingUserProfile()
    {
        // Arrange
        const string id = "user-001";
        const string email = "test@example.com";
        const string displayName = "Test User";
        IReadOnlyList<string> roles = ["admin", "user"];

        // Act
        UserProfile profile = new(id, email, displayName, roles);

        // Assert
        ItShouldStoreCorrectValues(profile, id, email, displayName, roles);
    }

    private static void ItShouldStoreCorrectValues(UserProfile profile, string expectedId, string expectedEmail, string expectedDisplayName, IReadOnlyList<string> expectedRoles)
    {
        profile.Id.ShouldBe(expectedId);
        profile.Email.ShouldBe(expectedEmail);
        profile.DisplayName.ShouldBe(expectedDisplayName);
        profile.Roles.ShouldBe(expectedRoles);
    }

    [Fact]
    public void GivenTwoProfilesWithSameData_WhenComparing()
    {
        // Arrange
        IReadOnlyList<string> roles = ["admin"];

        // Act
        UserProfile profile1 = new("user-001", "test@example.com", "Test User", roles);
        UserProfile profile2 = new("user-001", "test@example.com", "Test User", roles);

        // Assert
        ItShouldBeEqual(profile1, profile2);
    }

    private static void ItShouldBeEqual(UserProfile profile1, UserProfile profile2)
    {
        profile1.ShouldBe(profile2);
    }

    [Fact]
    public void GivenTwoProfilesWithDifferentData_WhenComparing()
    {
        // Arrange
        IReadOnlyList<string> roles = ["user"];

        // Act
        UserProfile profile1 = new("user-001", "alice@example.com", "Alice", roles);
        UserProfile profile2 = new("user-002", "bob@example.com", "Bob", roles);

        // Assert
        ItShouldNotBeEqual(profile1, profile2);
    }

    private static void ItShouldNotBeEqual(UserProfile profile1, UserProfile profile2)
    {
        profile1.ShouldNotBe(profile2);
    }
}
