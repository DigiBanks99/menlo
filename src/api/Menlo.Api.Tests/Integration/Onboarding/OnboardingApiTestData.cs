using Menlo.Application.Auth;
using Menlo.Application.Onboarding;
using Menlo.Lib.Auth.Entities;
using Menlo.Lib.Auth.ValueObjects;
using Menlo.Lib.Common.ValueObjects;
using Menlo.Lib.Onboarding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Menlo.Api.Tests.Integration.Onboarding;

internal static class OnboardingApiTestData
{
    public static async Task<User> ProvisionUserAsync(
        IServiceProvider services,
        string externalId,
        string email,
        string displayName,
        bool completeOnboarding = false,
        string? householdName = null)
    {
        using IServiceScope scope = services.CreateScope();
        UserProvisioningService provisioningService = scope.ServiceProvider.GetRequiredService<UserProvisioningService>();
        IUserContext userContext = scope.ServiceProvider.GetRequiredService<IUserContext>();
        IHouseholdContext householdContext = scope.ServiceProvider.GetRequiredService<IHouseholdContext>();
        IOnboardingContext onboardingContext = scope.ServiceProvider.GetRequiredService<IOnboardingContext>();

        await provisioningService.ProvisionOrUpdateAsync(externalId, email, displayName, TestContext.Current!.Execution.CancellationToken);

        ExternalUserId normalizedExternalId = new(externalId);
        User user = await userContext.Users
            .SingleAsync(candidate => candidate.ExternalId == normalizedExternalId, TestContext.Current!.Execution.CancellationToken);

        if (!completeOnboarding)
        {
            return user;
        }

        Household household = Household.Create(householdName ?? $"Household {Guid.NewGuid():N}").Value;
        householdContext.Households.Add(household);
        user.AssignToHousehold(household.Id.Value);

        OnboardingState onboardingState = await onboardingContext.OnboardingStates
            .SingleAsync(state => state.UserId == user.Id, TestContext.Current!.Execution.CancellationToken);
        onboardingState.CompleteTask(
            OnboardingTaskType.SelectedHousehold,
            new Dictionary<string, object>
            {
                ["householdId"] = household.Id.Value.ToString()
            });

        await householdContext.SaveChangesAsync(TestContext.Current!.Execution.CancellationToken);
        await userContext.SaveChangesAsync(TestContext.Current!.Execution.CancellationToken);
        await onboardingContext.SaveChangesAsync(TestContext.Current!.Execution.CancellationToken);

        return user;
    }

    public static async Task RemoveOnboardingStateAsync(IServiceProvider services, UserId userId)
    {
        using IServiceScope scope = services.CreateScope();
        IOnboardingContext onboardingContext = scope.ServiceProvider.GetRequiredService<IOnboardingContext>();

        OnboardingState onboardingState = await onboardingContext.OnboardingStates
            .SingleAsync(state => state.UserId == userId, TestContext.Current!.Execution.CancellationToken);
        onboardingContext.OnboardingStates.Remove(onboardingState);
        await onboardingContext.SaveChangesAsync(TestContext.Current!.Execution.CancellationToken);
    }

    public static async Task<Household> CreateHouseholdAsync(IServiceProvider services, string name)
    {
        using IServiceScope scope = services.CreateScope();
        IHouseholdContext householdContext = scope.ServiceProvider.GetRequiredService<IHouseholdContext>();

        Household household = Household.Create(name).Value;
        householdContext.Households.Add(household);
        await householdContext.SaveChangesAsync(TestContext.Current!.Execution.CancellationToken);
        return household;
    }

    public static async Task<User> GetUserAsync(IServiceProvider services, UserId userId)
    {
        using IServiceScope scope = services.CreateScope();
        IUserContext userContext = scope.ServiceProvider.GetRequiredService<IUserContext>();

        return await userContext.Users
            .AsNoTracking()
            .SingleAsync(user => user.Id == userId, TestContext.Current!.Execution.CancellationToken);
    }

    public static async Task<OnboardingState> GetOnboardingStateAsync(IServiceProvider services, UserId userId)
    {
        using IServiceScope scope = services.CreateScope();
        IOnboardingContext onboardingContext = scope.ServiceProvider.GetRequiredService<IOnboardingContext>();

        return await onboardingContext.OnboardingStates
            .AsNoTracking()
            .SingleAsync(state => state.UserId == userId, TestContext.Current!.Execution.CancellationToken);
    }
}
