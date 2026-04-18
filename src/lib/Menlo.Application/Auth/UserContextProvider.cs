using CSharpFunctionalExtensions;
using Menlo.Lib.Auth.Abstractions;
using Menlo.Lib.Auth.Errors;
using Menlo.Lib.Auth.Models;
using Menlo.Lib.Auth.ValueObjects;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Menlo.Application.Auth;

/// <summary>
/// Resolves the current authenticated user's identity into a lightweight <see cref="UserContext"/> projection.
/// Claim precedence: sub → oid → nameidentifier → appid → azp
/// </summary>
public sealed class UserContextProvider(IHttpContextAccessor httpContextAccessor, IUserContext userContext)
    : IUserContextProvider
{
    private static readonly string[] ClaimPrecedence =
    [
        "sub",
        "oid",
        ClaimTypes.NameIdentifier,
        "appid",
        "azp"
    ];

    public async Task<Result<UserContext, AuthError>> GetUserContextAsync(CancellationToken cancellationToken = default)
    {
        ClaimsPrincipal? principal = httpContextAccessor.HttpContext?.User;

        if (principal is null)
        {
            return new UnauthenticatedError();
        }

        string? externalIdValue = null;
        foreach (string claimType in ClaimPrecedence)
        {
            externalIdValue = principal.FindFirstValue(claimType);
            if (!string.IsNullOrEmpty(externalIdValue))
            {
                break;
            }
        }

        if (string.IsNullOrEmpty(externalIdValue))
        {
            return new UnauthenticatedError();
        }

        ExternalUserId externalId = new(externalIdValue);

        Lib.Auth.Entities.User? user = await userContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.ExternalId == externalId, cancellationToken);

        if (user is null)
        {
            return new UserNotFoundError(externalIdValue);
        }

        if (user.HouseholdId is null)
        {
            return new UserNotAssignedToHouseholdError(externalIdValue);
        }

        return new UserContext(user.Id, user.HouseholdId.Value);
    }
}
