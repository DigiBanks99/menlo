using Menlo.Application.Common;
using Menlo.Application.Onboarding;
using Menlo.Lib.Auth.Entities;
using Menlo.Lib.Auth.ValueObjects;
using Menlo.Lib.Onboarding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Menlo.Api.Tests.Integration.Onboarding;

[Collection("Onboarding")]
public sealed class OnboardingStateIntegrationTests(OnboardingPersistenceFixture fixture)
{
    [Fact]
    public async Task GivenNewUser_WhenQueryingOnboardingState_StateExists()
    {
        (string externalId, string email, string displayName) = CreateProvisioningInput();

        using IServiceScope scope = fixture.Services.CreateScope();
        UserProvisioningService provisioningService = scope.ServiceProvider.GetRequiredService<UserProvisioningService>();
        await provisioningService.ProvisionOrUpdateAsync(externalId, email, displayName, TestContext.Current.CancellationToken);

        using IServiceScope readScope = fixture.Services.CreateScope();
        MenloDbContext db = readScope.ServiceProvider.GetRequiredService<MenloDbContext>();
        ExternalUserId normalizedExternalId = new(externalId);
        User user = await db.Users
            .AsNoTracking()
            .SingleAsync(candidate => candidate.ExternalId == normalizedExternalId, TestContext.Current.CancellationToken);
        OnboardingState? onboardingState = await db.OnboardingStates
            .AsNoTracking()
            .FirstOrDefaultAsync(state => state.UserId == user.Id, TestContext.Current.CancellationToken);

        ItShouldHaveProvisionedState(onboardingState, user.Id);
    }

    [Fact]
    public async Task GivenOnboardingState_WhenCompleteTaskCalled_StateIsUpdated()
    {
        (string externalId, string email, string displayName) = CreateProvisioningInput();

        using IServiceScope scope = fixture.Services.CreateScope();
        UserProvisioningService provisioningService = scope.ServiceProvider.GetRequiredService<UserProvisioningService>();
        await provisioningService.ProvisionOrUpdateAsync(externalId, email, displayName, TestContext.Current.CancellationToken);

        MenloDbContext db = scope.ServiceProvider.GetRequiredService<MenloDbContext>();
        ExternalUserId normalizedExternalId = new(externalId);
        User user = await db.Users
            .SingleAsync(candidate => candidate.ExternalId == normalizedExternalId, TestContext.Current.CancellationToken);
        OnboardingState onboardingState = await db.OnboardingStates
            .SingleAsync(state => state.UserId == user.Id, TestContext.Current.CancellationToken);

        onboardingState.CompleteTask(
            OnboardingTaskType.SelectedHousehold,
            new Dictionary<string, object>
            {
                ["householdId"] = Guid.NewGuid().ToString()
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        OnboardingState persistedState = await db.OnboardingStates
            .AsNoTracking()
            .SingleAsync(state => state.UserId == user.Id, TestContext.Current.CancellationToken);

        ItShouldShowCompletedHouseholdSelection(persistedState);
    }

    private static (string externalId, string email, string displayName) CreateProvisioningInput()
    {
        string uniqueSuffix = Guid.NewGuid().ToString("N");
        return ($"external-{uniqueSuffix}", $"user-{uniqueSuffix}@menlo.app", $"User {uniqueSuffix}");
    }

    private static void ItShouldHaveProvisionedState(OnboardingState? onboardingState, Menlo.Lib.Common.ValueObjects.UserId expectedUserId)
    {
        onboardingState.ShouldNotBeNull();
        onboardingState.UserId.ShouldBe(expectedUserId);
        onboardingState.HasSelectedHousehold.ShouldBeFalse();
    }

    private static void ItShouldShowCompletedHouseholdSelection(OnboardingState onboardingState)
    {
        onboardingState.HasSelectedHousehold.ShouldBeTrue();
        onboardingState.IsFullyOnboarded.ShouldBeTrue();
        onboardingState.CompletedTasks.Count.ShouldBe(1);
    }
}
