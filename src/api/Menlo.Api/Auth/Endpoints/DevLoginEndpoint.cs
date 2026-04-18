using Menlo.Api.Auth.Policies;
using Menlo.Application.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Menlo.Api.Auth.Endpoints;

/// <summary>
/// Development-only endpoint to bypass authentication for local testing.
/// Only registered when running in Development mode with Features:DevAuth=true.
/// </summary>
public static class DevLoginEndpoint
{
    extension(RouteGroupBuilder group)
    {
        /// <summary>
        /// Maps the dev login endpoint to the route group.
        /// </summary>
        public RouteGroupBuilder MapDevLogin()
        {
            group.MapPost("/dev-login", Handle)
                .WithName("DevLogin")
                .WithSummary("Development-only: signs in as a configured dev user and seeds the user if absent")
                .AllowAnonymous()
                .Produces<DevLoginResponse>(StatusCodes.Status200OK);

            return group;
        }
    }

    private static async Task<IResult> Handle(
        HttpContext context,
        MenloDbContext db,
        DevLoginRequest? request,
        CancellationToken cancellationToken)
    {
        string externalId = request?.ExternalId ?? "dev-menlo-user";
        string email = request?.Email ?? "dev@menlo.local";
        string displayName = request?.DisplayName ?? "Dev User";
        string role = request?.Role ?? MenloPolicies.Roles.Admin;

        // Upsert the dev user; assign to the first available household if not already assigned.
        await db.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO shared.users (id, external_id, email, display_name, last_login_at, household_id, is_deleted)
            VALUES (
                gen_random_uuid(),
                {externalId},
                {email},
                {displayName},
                now(),
                (SELECT id FROM shared.households WHERE is_deleted = false ORDER BY name LIMIT 1),
                false
            )
            ON CONFLICT (external_id) DO UPDATE SET
                email               = EXCLUDED.email,
                display_name        = EXCLUDED.display_name,
                last_login_at       = now(),
                household_id        = COALESCE(shared.users.household_id, EXCLUDED.household_id),
                is_deleted          = false
            """,
            cancellationToken);

        ClaimsIdentity identity = new(
            [
                new Claim("sub", externalId),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Name, displayName),
                new Claim(ClaimTypes.Role, role),
            ],
            CookieAuthenticationDefaults.AuthenticationScheme);

        ClaimsPrincipal principal = new(identity);

        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        return Results.Ok(new DevLoginResponse(externalId, email, displayName, role));
    }
}

/// <summary>Request body for the dev login endpoint.</summary>
public sealed record DevLoginRequest(string? ExternalId, string? Email, string? DisplayName, string? Role);

/// <summary>Response body confirming the signed-in dev identity.</summary>
public sealed record DevLoginResponse(string ExternalId, string Email, string DisplayName, string Role);
