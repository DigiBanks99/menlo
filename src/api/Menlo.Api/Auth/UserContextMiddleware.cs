using CSharpFunctionalExtensions;
using Menlo.Application.Auth;
using Menlo.Application.Users;
using Menlo.Lib.Auth.Entities;
using Menlo.Lib.Auth.Errors;
using Menlo.Lib.Auth.Models;
using System.Security.Claims;

namespace Menlo.Api.Auth;

/// <summary>
/// Makes sure the signed-in caller has a <c>User</c> record and records it on the scoped <see cref="CurrentUser"/>,
/// so the audit and soft-delete stamp factories stamp the real user. Sign-in provisions the user; this is the safety net
/// when that step didn't run or failed.
/// <para>
/// It must run after authentication and authorization and before anything resolves the request's DbContext or stamp
/// factories (endpoint binding does), because the factories read the user when they are built. Provisioning runs in its own
/// scope so it doesn't build the request scope's DbContext early. Requests with no signed-in user pass through and are
/// stamped as the system user.
/// </para>
/// </summary>
public sealed class UserContextMiddleware(RequestDelegate next, ILogger<UserContextMiddleware> logger)
{
    /// <summary>Machine-readable code of the 403 returned when the claims can't make a user.</summary>
    public const string UserNotProvisionedCode = "USER_NOT_PROVISIONED";

    public async Task InvokeAsync(
        HttpContext context,
        CurrentUser currentUser,
        IServiceScopeFactory scopeFactory)
    {
        ClaimsPrincipal principal = context.User;
        if (principal.Identity?.IsAuthenticated != true)
        {
            await next(context);
            return;
        }

        string? externalId = CurrentUserLookup.FindExternalId(principal);
        string? email = principal.FindFirstValue(ClaimTypes.Email);

        if (string.IsNullOrWhiteSpace(externalId) || string.IsNullOrWhiteSpace(email))
        {
            logger.MissingClaim(string.IsNullOrWhiteSpace(externalId) ? "external id" : "email");
            await NotProvisioned().ExecuteAsync(context);
            return;
        }

        string displayName = principal.FindFirstValue(ClaimTypes.Name) ?? email;

        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        IUserProvisioningService provisioningService = scope.ServiceProvider.GetRequiredService<IUserProvisioningService>();

        Result<User, AuthError> user = await provisioningService
            .ProvisionOrUpdateAsync(externalId, email, displayName, context.RequestAborted)
            .TapError(error => logger.ProvisioningFailed(error.Code, error.Message));

        if (user.IsFailure)
        {
            await NotProvisioned().ExecuteAsync(context);
            return;
        }

        currentUser.Set(new UserContext(user.Value.Id));

        await next(context);
    }

    private static Microsoft.AspNetCore.Http.IResult NotProvisioned() =>
        Results.Problem(
            title: "User is not provisioned",
            detail: "Your sign-in does not carry enough information to create your Menlo user.",
            statusCode: StatusCodes.Status403Forbidden,
            extensions: new Dictionary<string, object?> { ["code"] = UserNotProvisionedCode });
}

public static class UserContextMiddlewareExtensions
{
    /// <summary>
    /// Adds <see cref="UserContextMiddleware"/> for <c>/api</c> requests. Call it after <c>UseAuthorization()</c> and before
    /// endpoints run.
    /// </summary>
    public static IApplicationBuilder UseMenloUserContext(this IApplicationBuilder app) =>
        app.UseWhen(
            context => context.Request.Path.StartsWithSegments("/api"),
            branch => branch.UseMiddleware<UserContextMiddleware>());
}
