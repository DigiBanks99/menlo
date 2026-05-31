using Menlo.Api.Auth.Policies;
using Menlo.Application.Auth;
using Menlo.Application.Onboarding;
using Menlo.Lib.Auth.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Data.Common;
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

    private static async Task<IResult> Handle(
        ClaimsPrincipal user,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        if (user.Identity?.IsAuthenticated != true)
        {
            return Results.Unauthorized();
        }

        Menlo.Lib.Auth.Entities.User? currentUser = null;
        IOnboardingContext? onboardingContext = null;

        try
        {
            IUserContext userContext = serviceProvider.GetRequiredService<IUserContext>();
            onboardingContext = serviceProvider.GetRequiredService<IOnboardingContext>();

            currentUser = await CurrentUserLookup.FindUserAsync(
                user,
                userContext.Users.AsNoTracking(),
                cancellationToken);
        }
        catch (Exception ex) when (ex is InvalidOperationException or DbException)
        {
            currentUser = null;
        }

        string[] roles = [.. user.FindAll(ClaimTypes.Role).Select(claim => claim.Value)];
        string defaultEmail = user.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        string defaultDisplayName = user.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
        string defaultId = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? CurrentUserLookup.FindExternalId(user)
            ?? string.Empty;

        OnboardingInfo onboarding = new(
            IsComplete: false,
            PendingTasks: ["SelectHousehold"]);

        if (currentUser is not null && onboardingContext is not null)
        {
            try
            {
                var onboardingState = await onboardingContext.OnboardingStates
                    .AsNoTracking()
                    .FirstOrDefaultAsync(state => state.UserId == currentUser.Id, cancellationToken);

                bool isComplete = onboardingState?.HasSelectedHousehold ?? false;
                onboarding = new OnboardingInfo(
                    IsComplete: isComplete,
                    PendingTasks: isComplete ? [] : ["SelectHousehold"]);
            }
            catch (Exception ex) when (ex is InvalidOperationException or DbException)
            {
                onboarding = new OnboardingInfo(
                    IsComplete: false,
                    PendingTasks: ["SelectHousehold"]);
            }
        }

        UserProfile profile = new(
            Id: currentUser?.Id.Value.ToString() ?? defaultId,
            Email: currentUser?.Email ?? defaultEmail,
            DisplayName: currentUser?.DisplayName ?? defaultDisplayName,
            Roles: roles,
            Onboarding: onboarding);

        return Results.Ok(profile);
    }
}


