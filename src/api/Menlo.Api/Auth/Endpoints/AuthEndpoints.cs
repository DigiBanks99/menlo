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
    /// <returns>The endpoint route builder for chaining.</returns>
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGroup("/auth")
            .WithTags("Authentication")
            .MapLogin()
            .MapLogout()
            .MapGetUser();

        return app;
    }
}
