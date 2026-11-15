using CSharpFunctionalExtensions;
using Menlo.Application.Users;
using Menlo.Lib.Auth.Entities;
using Menlo.Lib.Auth.Errors;
using System.Security.Claims;

namespace Menlo.Api.Auth;

/// <summary>
/// Endpoint filter for the <c>/api</c> group: makes sure the signed-in caller has a <c>User</c> record.
/// Sign-in provisions it; this is the safety net when that step didn't run or failed.
/// Runs after authentication and authorization, before the handler.
/// </summary>
public sealed class EnsureUserProvisionedFilter(
    IUserProvisioningService provisioningService,
    ILogger<EnsureUserProvisionedFilter> logger) : IEndpointFilter
{
    /// <summary>Machine-readable code of the 403 returned when the claims can't make a user.</summary>
    public const string UserNotProvisionedCode = "USER_NOT_PROVISIONED";

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        ClaimsPrincipal principal = context.HttpContext.User;
        string? externalId = CurrentUserLookup.FindExternalId(principal);
        string? email = principal.FindFirstValue(ClaimTypes.Email);

        if (string.IsNullOrWhiteSpace(externalId) || string.IsNullOrWhiteSpace(email))
        {
            logger.MissingClaim(string.IsNullOrWhiteSpace(externalId) ? "external id" : "email");
            return NotProvisioned();
        }

        string displayName = principal.FindFirstValue(ClaimTypes.Name) ?? email;

        Result<User, AuthError> user = await provisioningService
            .ProvisionOrUpdateAsync(
                externalId,
                email,
                displayName,
                context.HttpContext.RequestAborted)
            .TapError(error => logger.ProvisioningFailed(error.Code, error.Message));

        if (user.IsFailure)
        {
            return NotProvisioned();
        }

        return await next(context);
    }

    private static Microsoft.AspNetCore.Http.IResult NotProvisioned() =>
        Results.Problem(
            title: "User is not provisioned",
            detail: "Your sign-in does not carry enough information to create your Menlo user.",
            statusCode: StatusCodes.Status403Forbidden,
            extensions: new Dictionary<string, object?> { ["code"] = UserNotProvisionedCode });
}
