using CSharpFunctionalExtensions;
using Menlo.Application.Users;
using Menlo.Lib.Auth.Entities;
using Menlo.Lib.Auth.Errors;
using Menlo.Lib.Common.ValueObjects;
using System.Security.Claims;

namespace Menlo.Api.Auth;

/// <summary>
/// The signed-in user's <see cref="UserId"/>, carried as a claim in the session cookie. Sign-in puts it there once, so
/// requests read it from the cookie and never look the user up.
/// </summary>
internal static class UserSessionClaims
{
    /// <summary>Claim type that holds the Menlo <see cref="UserId"/>.</summary>
    public const string UserIdClaimType = "menlo_user_id";

    /// <summary>Reads the user id claim, or <c>null</c> when it is missing or not a GUID.</summary>
    public static UserId? FindUserId(ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(UserIdClaimType), out Guid id) && id != Guid.Empty
            ? new UserId(id)
            : null;

    /// <summary>Whether the principal already carries a usable user id claim.</summary>
    public static bool HasUserId(ClaimsPrincipal principal) => FindUserId(principal) is not null;

    /// <summary>Adds the user id claim to the principal's identity.</summary>
    public static void AddUserId(ClaimsPrincipal principal, UserId userId)
    {
        ClaimsIdentity identity = principal.Identities.FirstOrDefault(i => i.IsAuthenticated)
            ?? principal.Identities.First();
        identity.AddClaim(new Claim(UserIdClaimType, userId.Value.ToString()));
    }

    /// <summary>
    /// Finds or creates the user for the principal's claims, in its own scope so the request's DbContext isn't built early.
    /// </summary>
    public static async Task<Result<User, AuthError>> ProvisionAsync(
        IServiceScopeFactory scopeFactory,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        string? externalId = CurrentUserLookup.FindExternalId(principal);
        string? email = principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(externalId) || string.IsNullOrWhiteSpace(email))
        {
            return new InvalidUserDataError("The sign-in claims have no external id or email.");
        }

        string displayName = principal.FindFirstValue(ClaimTypes.Name) ?? email;

        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        return await scope.ServiceProvider
            .GetRequiredService<IUserProvisioningService>()
            .ProvisionAsync(externalId, email, displayName, cancellationToken);
    }
}
