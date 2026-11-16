using Menlo.Api.Ui.Shared;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Menlo.Api.Ui.SignIn;

internal static class SignInEndpoints
{
    public static IEndpointRouteBuilder MapSignInPage(this IEndpointRouteBuilder app)
    {
        app.MapGet("/sign-in", Page).AllowAnonymous();
        return app;
    }

    private static Results<RedirectHttpResult, RazorComponentResult<SignInPage>> Page(
        HttpContext context,
        string? returnUrl = "/",
        string? error = null)
    {
        // Only local addresses, so the sign-in page can't be used to bounce someone to another site
        string target = IsLocalUrl(returnUrl) ? returnUrl! : "/";

        if (context.User.Identity?.IsAuthenticated == true)
        {
            return TypedResults.LocalRedirect(target);
        }

        SignInPageModel model = new(
            LoginUrl: $"/auth/login?returnUrl={Uri.EscapeDataString(target)}",
            Failed: !string.IsNullOrEmpty(error));

        return Render.Page<SignInPage>(context, model);
    }

    private static bool IsLocalUrl(string? url) =>
        !string.IsNullOrEmpty(url)
        && url[0] == '/'
        && (url.Length == 1 || (url[1] != '/' && url[1] != '\\'));
}
