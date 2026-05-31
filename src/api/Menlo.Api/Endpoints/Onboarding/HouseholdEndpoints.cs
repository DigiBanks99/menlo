using Menlo.Api.Auth;
using Menlo.Api.Auth.Policies;
using Menlo.Application.Auth;
using Menlo.Application.Onboarding;
using Menlo.Lib.Auth.Entities;
using Menlo.Lib.Common.ValueObjects;
using Menlo.Lib.Onboarding;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Menlo.Api.Endpoints.Onboarding;

public static class HouseholdEndpoints
{
    public static RouteGroupBuilder MapHouseholdEndpoints(this RouteGroupBuilder group)
    {
        RouteGroupBuilder households = group
            .MapGroup("/households")
            .WithTags("Households")
            .RequireAuthorization(MenloPolicies.RequireAuthenticated);

        households.MapGet(string.Empty, ListHouseholds)
            .WithName("ListHouseholds")
            .WithSummary("List available households");

        households.MapPost(string.Empty, CreateHousehold)
            .WithName("CreateHousehold")
            .WithSummary("Create a new household and mark onboarding complete")
            .Produces(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status409Conflict);

        households.MapPost("/{id:guid}/join", JoinHousehold)
            .WithName("JoinHousehold")
            .WithSummary("Join an existing household and mark onboarding complete")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        return group;
    }

    private static async Task<IResult> ListHouseholds(
        IHouseholdContext householdContext,
        CancellationToken cancellationToken)
    {
        HouseholdSummaryResponse[] households = await householdContext.Households
            .AsNoTracking()
            .OrderBy(household => household.Name)
            .Select(household => new HouseholdSummaryResponse(
                household.Id.Value,
                household.Name))
            .ToArrayAsync(cancellationToken);

        return Results.Ok(new HouseholdListResponse(households));
    }

    private static async Task<IResult> CreateHousehold(
        CreateHouseholdRequest request,
        ClaimsPrincipal principal,
        IUserContext userContext,
        IHouseholdContext householdContext,
        IOnboardingContext onboardingContext,
        CancellationToken cancellationToken)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return Results.Unauthorized();
        }

        string householdName = request.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(householdName) || householdName.Length > 100)
        {
            return Results.BadRequest(new { error = "Household name must be between 1 and 100 characters." });
        }

        User? user = await CurrentUserLookup.FindUserAsync(principal, userContext.Users, cancellationToken);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        bool householdExists = await householdContext.Households
            .AsNoTracking()
            .AnyAsync(household => household.Name.ToLower() == householdName.ToLower(), cancellationToken);

        if (householdExists)
        {
            return Results.Conflict(new { error = "A household with this name already exists." });
        }

        var householdResult = Household.Create(householdName);
        if (householdResult.IsFailure)
        {
            return Results.BadRequest(new { error = householdResult.Error.Message });
        }

        Household household = householdResult.Value;
        householdContext.Households.Add(household);
        user.AssignToHousehold(household.Id.Value);

        OnboardingState? onboardingState = await onboardingContext.OnboardingStates
            .FirstOrDefaultAsync(state => state.UserId == user.Id, cancellationToken);

        onboardingState?.CompleteTask(
            OnboardingTaskType.SelectedHousehold,
            new Dictionary<string, object>
            {
                ["householdId"] = household.Id.Value.ToString()
            });

        await householdContext.SaveChangesAsync(cancellationToken);
        await userContext.SaveChangesAsync(cancellationToken);
        await onboardingContext.SaveChangesAsync(cancellationToken);

        return Results.Created(
            $"/api/households/{household.Id.Value}",
            new HouseholdSummaryResponse(household.Id.Value, household.Name));
    }

    private static async Task<IResult> JoinHousehold(
        Guid id,
        ClaimsPrincipal principal,
        IUserContext userContext,
        IHouseholdContext householdContext,
        IOnboardingContext onboardingContext,
        CancellationToken cancellationToken)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return Results.Unauthorized();
        }

        User? user = await CurrentUserLookup.FindUserAsync(principal, userContext.Users, cancellationToken);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        HouseholdId householdId = new(id);
        Household? household = await householdContext.Households
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == householdId, cancellationToken);

        if (household is null)
        {
            return Results.NotFound(new { error = "Household not found." });
        }

        user.AssignToHousehold(household.Id.Value);

        OnboardingState? onboardingState = await onboardingContext.OnboardingStates
            .FirstOrDefaultAsync(state => state.UserId == user.Id, cancellationToken);

        onboardingState?.CompleteTask(
            OnboardingTaskType.SelectedHousehold,
            new Dictionary<string, object>
            {
                ["householdId"] = household.Id.Value.ToString()
            });

        await userContext.SaveChangesAsync(cancellationToken);
        await onboardingContext.SaveChangesAsync(cancellationToken);

        return Results.NoContent();
    }
}

public sealed record CreateHouseholdRequest(string Name);

public sealed record HouseholdSummaryResponse(Guid Id, string Name);

public sealed record HouseholdListResponse(IReadOnlyList<HouseholdSummaryResponse> Households);
