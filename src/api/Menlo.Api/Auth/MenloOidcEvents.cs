using CSharpFunctionalExtensions;
using Menlo.Application.Users;
using Menlo.Lib.Auth.Entities;
using Menlo.Lib.Auth.Errors;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using System.Security.Claims;

namespace Menlo.Api.Auth;

/// <summary>
/// Provisions the user when they sign in or their session is validated, and records the user's id as a claim in the
/// session cookie so requests never have to look the user up.
/// </summary>
public sealed class MenloOidcEvents(IServiceProvider serviceProvider)
{
    public async Task OnTokenValidated(TokenValidatedContext context)
    {
        string? externalId = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? context.Principal?.FindFirst("oid")?.Value
            ?? context.Principal?.FindFirst("sub")?.Value;

        if (context.Principal is null || string.IsNullOrWhiteSpace(externalId))
        {
            return;
        }

        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();
        UserProvisioningService provisioningService = scope.ServiceProvider.GetRequiredService<UserProvisioningService>();

        string email = context.Principal.FindFirst(ClaimTypes.Email)?.Value ?? "unknown@tenant.onmicrosoft.com";
        string displayName = context.Principal.FindFirst(ClaimTypes.Name)?.Value ?? email;

        Result<User, AuthError> result = await provisioningService
            .ProvisionAsync(externalId, email, displayName, context.HttpContext.RequestAborted);
        if (result.IsFailure)
        {
            throw new InvalidOperationException(result.Error.Message);
        }

        UserSessionClaims.AddUserId(context.Principal, result.Value.Id);
    }

    /// <summary>
    /// Covers sessions that were signed in before the user id claim existed, or whose provisioning didn't finish: the first
    /// time such a session is validated, the user is provisioned and the claim is added and the cookie renewed. Sessions that
    /// already carry the claim cost nothing. If the user can't be provisioned the claim stays off, and
    /// <see cref="UserContextMiddleware"/> answers 403.
    /// </summary>
    public static async Task OnValidatePrincipal(CookieValidatePrincipalContext context)
    {
        ClaimsPrincipal? principal = context.Principal;
        if (principal?.Identity?.IsAuthenticated != true || UserSessionClaims.HasUserId(principal))
        {
            return;
        }

        Result<User, AuthError> result = await UserSessionClaims.ProvisionAsync(
            context.HttpContext.RequestServices.GetRequiredService<IServiceScopeFactory>(),
            principal,
            context.HttpContext.RequestAborted);
        if (result.IsFailure)
        {
            return;
        }

        UserSessionClaims.AddUserId(principal, result.Value.Id);
        context.ReplacePrincipal(principal);
        context.ShouldRenew = true;
    }
}
