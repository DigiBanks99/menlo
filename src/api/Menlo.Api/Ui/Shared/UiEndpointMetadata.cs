using Menlo.Api.Ui.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace Menlo.Api.Ui.Shared;

/// <summary>
/// Marks an endpoint as part of the HTML UI. The marker switches on the UI's handling of
/// sign-in redirects, anti-forgery validation and user stamping for that endpoint.
/// </summary>
internal sealed class UiEndpointMetadata
{
    public static readonly UiEndpointMetadata Instance = new();

    /// <summary>
    /// Whether the request was routed to a UI endpoint.
    /// </summary>
    public static bool IsOn(HttpContext context) =>
        context.GetEndpoint()?.Metadata.GetMetadata<UiEndpointMetadata>() is not null;
}

/// <summary>
/// Decides what a person sees when a UI endpoint refuses them. Someone who isn't signed in goes to
/// the sign-in page (a fragment request gets a 401, and menlo.js sends the browser there). Someone
/// without the right role gets the "no access" page. API endpoints keep the default behaviour.
/// </summary>
internal sealed class UiAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _default = new();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Succeeded || !UiEndpointMetadata.IsOn(context))
        {
            await _default.HandleAsync(next, context, policy, authorizeResult);
            return;
        }

        if (authorizeResult.Challenged)
        {
            if (HxRequest.IsFragment(context.Request))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            string returnUrl = context.Request.Path + context.Request.QueryString;

            if (ArrivedFromAnotherSite(context.Request))
            {
                await WriteSameSiteRetryAsync(context, returnUrl);
                return;
            }

            context.Response.Redirect($"/sign-in?returnUrl={Uri.EscapeDataString(returnUrl)}");
            return;
        }

        if (HxRequest.IsFragment(context.Request))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        await Render.Page<ForbiddenPage>(context, statusCode: StatusCodes.Status403Forbidden).ExecuteAsync(context);
    }

    // The session cookie is SameSite=Strict, so the browser leaves it out of a navigation that another
    // site started: a link from an email, or the redirect back from the Microsoft sign-in. The person
    // may well be signed in. Browsers say where a navigation came from in Sec-Fetch-Site.
    private static bool ArrivedFromAnotherSite(HttpRequest request) =>
        HttpMethods.IsGet(request.Method)
        && string.Equals(request.Headers["Sec-Fetch-Site"], "cross-site", StringComparison.OrdinalIgnoreCase);

    // Answers with a tiny page that reloads the same address. That reload starts on this site, so the
    // browser sends the cookie, and the request is then handled normally (signed in, or sent to sign-in).
    // It can't loop: the retry is same-origin, not cross-site.
    private static async Task WriteSameSiteRetryAsync(HttpContext context, string url)
    {
        string encoded = System.Net.WebUtility.HtmlEncode(url);

        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = "text/html; charset=utf-8";
        context.Response.Headers.CacheControl = "no-store";

        await context.Response.WriteAsync(
            $"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
            <meta charset="utf-8">
            <meta http-equiv="refresh" content="0;url={encoded}">
            <title>Menlo</title>
            </head>
            <body><a href="{encoded}">Continue to Menlo</a></body>
            </html>
            """);
    }
}
