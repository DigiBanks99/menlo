namespace Menlo.Api.Tests.Fixtures;

/// <summary>
/// Configuration options for the test authentication handler.
/// </summary>
public sealed class TestAuthHandlerOptions
{
    /// <summary>
    /// Gets or sets the roles to assign to the test user.
    /// </summary>
    public string[] Roles { get; set; } = ["Menlo.User"];

    /// <summary>
    /// Gets or sets whether to simulate an unauthenticated user.
    /// </summary>
    public bool SimulateUnauthenticated { get; set; }
}
