using Menlo.Api.Ui.Shared;
using Menlo.Application.Auth;
using Menlo.Lib.Auth.Models;
using Menlo.Lib.Common.ValueObjects;

namespace Menlo.Api.Auth;

/// <summary>
/// Records the signed-in caller on the scoped <see cref="CurrentUser"/>, so the audit and soft-delete stamp factories stamp
/// the real user. It only reads the user id claim that sign-in (or the first validation of an older session) put in the
/// session cookie: it never touches the database.
/// <para>
/// It must run after authentication and authorization and before anything resolves the request's DbContext or stamp
/// factories (endpoint binding does), because the factories read the user when they are built. Requests with no signed-in
/// user pass through and are stamped as the system user.
/// </para>
/// </summary>
public sealed class UserContextMiddleware(RequestDelegate next, ILogger<UserContextMiddleware> logger)
{
    /// <summary>Machine-readable code of the 403 returned when the session has no user id.</summary>
    public const string UserNotProvisionedCode = "USER_NOT_PROVISIONED";

    public async Task InvokeAsync(HttpContext context, CurrentUser currentUser)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await next(context);
            return;
        }

        UserId? userId = UserSessionClaims.FindUserId(context.User);
        if (userId is null)
        {
            logger.MissingUserId();
            await NotProvisioned().ExecuteAsync(context);
            return;
        }

        currentUser.Set(new UserContext(userId.Value));

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
    /// Adds <see cref="UserContextMiddleware"/> for <c>/api</c> requests and UI endpoints. Call it after <c>UseAuthorization()</c> and before
    /// endpoints run.
    /// </summary>
    public static IApplicationBuilder UseMenloUserContext(this IApplicationBuilder app) =>
        app.UseWhen(
            context => context.Request.Path.StartsWithSegments("/api") || UiEndpointMetadata.IsOn(context),
            branch => branch.UseMiddleware<UserContextMiddleware>());
}
