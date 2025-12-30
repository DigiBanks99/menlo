using Menlo.Api.Auth.Policies;
using Menlo.Lib.Auth.Models;
using System.Security.Claims;

namespace Menlo.Api.Auth.Endpoints;

/// <summary>
/// Returns the current user's profile.
/// </summary>
public static class GetUserEndpoint
{
    extension(RouteGroupBuilder group)
    {
        /// <summary>
        /// Maps the user endpoint.
        /// </summary>
        /// <param name="group">The route group builder.</param>
        public RouteGroupBuilder MapGetUser()
        {
            group.MapGet("/user", Handle)
                .WithName("GetCurrentUser")
                .WithSummary("Returns the current user's profile and roles")
                .RequireAuthorization(MenloPolicies.RequireAuthenticated)
                .Produces<UserProfile>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status401Unauthorized);
            return group;
        }
    }

    private static IResult Handle(ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated != true)
        {
            return Results.Unauthorized();
        }

        UserProfile profile = new(
            Id: user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty,
            Email: user.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
            DisplayName: user.FindFirstValue(ClaimTypes.Name) ?? string.Empty,
            Roles: user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList());

        return Results.Ok(profile);
    }
}
