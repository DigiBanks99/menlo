namespace Menlo.Api.Tests.Fixtures;

/// <summary>
/// Configuration options for the test authentication handler.
/// </summary>
public sealed class TestAuthHandlerOptions
{
    /// <summary>
    /// Gets or sets the authenticated external user identifier claim value.
    /// </summary>
    public string UserId { get; set; } = TestAuthHandler.DefaultUserId;

    /// <summary>
    /// Gets or sets the authenticated user's email claim value.
    /// </summary>
    public string Email { get; set; } = TestAuthHandler.DefaultEmail;

    /// <summary>
    /// Gets or sets the authenticated user's display name claim value.
    /// </summary>
    public string DisplayName { get; set; } = TestAuthHandler.DefaultName;

    /// <summary>
    /// Gets or sets the roles to assign to the test user.
    /// </summary>
    public string[] Roles { get; set; } = ["Menlo.User"];

    /// <summary>
    /// Gets or sets whether to simulate an unauthenticated user.
    /// </summary>
    public bool SimulateUnauthenticated { get; set; }
}


