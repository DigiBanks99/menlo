using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace Menlo.Api.Auth.Endpoints;

/// <summary>
/// Handles user logout.
/// </summary>
public static class LogoutEndpoint
{
    extension(RouteGroupBuilder group)
    {
        /// <summary>
        /// Maps the logout endpoint.
        /// </summary>
        /// <param name="group">The route group builder.</param>
        public RouteGroupBuilder MapLogout()
        {
            group.MapPost("/logout", Handle)
                .WithName("Logout")
                .WithSummary("Signs out the user")
                .RequireAuthorization();
            return group;
        }
    }

    private static IResult Handle(HttpContext context, string? returnUrl = "/")
    {
        AuthenticationProperties properties = new()
        {
            RedirectUri = returnUrl
        };

        return Results.SignOut(
            properties,
            [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme]);
    }
}
