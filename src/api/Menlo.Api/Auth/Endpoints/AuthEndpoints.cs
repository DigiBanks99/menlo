namespace Menlo.Api.Auth.Endpoints;

/// <summary>
/// Maps all authentication endpoints.
/// </summary>
public static class AuthEndpoints
{
    /// <summary>
    /// Maps authentication endpoints to the application.
    /// </summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <param name="env">The hosting environment; controls dev-only endpoint registration.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The endpoint route builder for chaining.</returns>
    public static IEndpointRouteBuilder MapAuthEndpoints(
        this IEndpointRouteBuilder app,
        IWebHostEnvironment env,
        IConfiguration configuration)
    {
        RouteGroupBuilder group = app.MapGroup("/auth")
            .WithTags("Authentication")
            .MapLogin()
            .MapLogout()
            .MapGetUser();

        if (env.IsDevelopment() && configuration.GetValue<bool>("Features:DevAuth"))
        {
            group.MapDevLogin();
        }

        return app;
    }
}


