namespace Menlo.Lib.Auth.Models;

/// <summary>
/// Data transfer object for the authenticated user's profile.
/// Returned by the /auth/user endpoint.
/// Does not expose any IdP-specific information (no tenant ID, client ID, etc.).
/// </summary>
/// <param name="Id">The internal user identifier.</param>
/// <param name="Email">The user's email address.</param>
/// <param name="DisplayName">The user's display name.</param>
/// <param name="Roles">The roles assigned to this user.</param>
public sealed record UserProfile(
    string Id,
    string Email,
    string DisplayName,
    IReadOnlyList<string> Roles);
