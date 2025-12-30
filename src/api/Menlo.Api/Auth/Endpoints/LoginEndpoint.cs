using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace Menlo.Api.Auth.Endpoints;

/// <summary>
/// Handles the OIDC login flow.
/// </summary>
public static class LoginEndpoint
{
    extension(RouteGroupBuilder group)
    {
        /// <summary>
        /// Maps the login endpoint.
        /// </summary>
        /// <param name="group">The route group builder.</param>
        public RouteGroupBuilder MapLogin()
        {
            group.MapGet("/login", Handle)
                .WithName("Login")
                .WithSummary("Initiates OIDC login flow")
                .AllowAnonymous();

            return group;
        }
    }

    private static IResult Handle(HttpContext context, string? returnUrl = "/")
    {
        // Prevent open redirect attacks - only allow relative URLs
        if (string.IsNullOrEmpty(returnUrl) || !Uri.IsWellFormedUriString(returnUrl, UriKind.Relative))
        {
            returnUrl = "/";
        }

        AuthenticationProperties properties = new()
        {
            RedirectUri = returnUrl
        };

        return Results.Challenge(properties, [OpenIdConnectDefaults.AuthenticationScheme]);
    }
}
