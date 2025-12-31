using Menlo.Lib.Auth.Errors;
using Shouldly;

namespace Menlo.Lib.Tests.Auth.Errors;

/// <summary>
/// Tests for authentication and authorization error types.
/// </summary>
public sealed class AuthErrorTests
{
    [Fact]
    public void GivenCodeAndMessage_WhenCreatingAuthError()
    {
        string code = "Auth.Test";
        string message = "Test error message";

        AuthError error = new(code, message);

        ItShouldHaveCode(error, code);
        ItShouldHaveMessage(error, message);
    }

    [Fact]
    public void GivenExternalId_WhenCreatingUserNotFoundError()
    {
        string externalId = "external-user-123";

        UserNotFoundError error = new(externalId);

        ItShouldHaveCode(error, "Auth.UserNotFound");
        ItShouldHaveMessageContaining(error, externalId);
        ItShouldHaveExternalId(error, externalId);
    }

    [Fact]
    public void GivenNoParameters_WhenCreatingUnauthenticatedError()
    {
        UnauthenticatedError error = new();

        ItShouldHaveCode(error, "Auth.Unauthenticated");
        ItShouldHaveMessageContaining(error, "not authenticated");
    }

    [Fact]
    public void GivenNoParameters_WhenCreatingUnauthorisedError()
    {
        UnauthorisedError error = new();

        ItShouldHaveCode(error, "Auth.Unauthorised");
        ItShouldHaveMessageContaining(error, "not authorised");
    }

    [Fact]
    public void GivenPolicy_WhenCreatingForbiddenError()
    {
        string policy = "Menlo.Admin";

        ForbiddenError error = new(policy);

        ItShouldHaveCode(error, "Auth.Forbidden");
        ItShouldHaveMessageContaining(error, policy);
        ItShouldHavePolicy(error, policy);
    }

    [Fact]
    public void GivenReason_WhenCreatingInvalidUserDataError()
    {
        string reason = "Email is invalid";

        InvalidUserDataError error = new(reason);

        ItShouldHaveCode(error, "Auth.InvalidUserData");
        ItShouldHaveMessageContaining(error, reason);
        ItShouldHaveReason(error, reason);
    }

    // Assertion Helpers
    private static void ItShouldHaveCode(AuthError error, string expectedCode)
    {
        error.Code.ShouldBe(expectedCode);
    }

    private static void ItShouldHaveMessage(AuthError error, string expectedMessage)
    {
        error.Message.ShouldBe(expectedMessage);
    }

    private static void ItShouldHaveMessageContaining(AuthError error, string expectedSubstring)
    {
        error.Message.ShouldContain(expectedSubstring);
    }

    private static void ItShouldHaveExternalId(UserNotFoundError error, string expectedExternalId)
    {
        error.ExternalId.ShouldBe(expectedExternalId);
    }

    private static void ItShouldHavePolicy(ForbiddenError error, string expectedPolicy)
    {
        error.Policy.ShouldBe(expectedPolicy);
    }

    private static void ItShouldHaveReason(InvalidUserDataError error, string expectedReason)
    {
        error.Reason.ShouldBe(expectedReason);
    }
}
