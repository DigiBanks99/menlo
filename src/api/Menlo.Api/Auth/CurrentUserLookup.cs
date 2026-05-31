using Menlo.Application.Auth;
using Menlo.Lib.Auth.Entities;
using Menlo.Lib.Auth.ValueObjects;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Menlo.Api.Auth;

internal static class CurrentUserLookup
{
    private static readonly string[] ClaimPrecedence =
    [
        "sub",
        "oid",
        ClaimTypes.NameIdentifier,
        "appid",
        "azp"
    ];

    public static string? FindExternalId(ClaimsPrincipal principal)
    {
        foreach (string claimType in ClaimPrecedence)
        {
            string? value = principal.FindFirstValue(claimType);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    public static Task<User?> FindUserAsync(
        ClaimsPrincipal principal,
        IUserContext userContext,
        CancellationToken cancellationToken = default) =>
        FindUserAsync(principal, userContext.Users, cancellationToken);

    public static async Task<User?> FindUserAsync(
        ClaimsPrincipal principal,
        IQueryable<User> users,
        CancellationToken cancellationToken = default)
    {
        string? externalId = FindExternalId(principal);
        if (string.IsNullOrWhiteSpace(externalId))
        {
            return null;
        }

        ExternalUserId normalizedExternalId = new(externalId);
        return await users.FirstOrDefaultAsync(user => user.ExternalId == normalizedExternalId, cancellationToken);
    }
}
