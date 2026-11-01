using Menlo.Application.Common;
using Menlo.Application.Onboarding;
using Menlo.Lib.Auth.Entities;
using Menlo.Lib.Auth.ValueObjects;
using Menlo.Lib.Onboarding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Menlo.Api.Tests.Integration.Onboarding;

[ClassDataSource<OnboardingPersistenceFixture>(Shared = SharedType.PerTestSession)]
[NotInParallel("Onboarding")]
public sealed class OnboardingStateIntegrationTests(OnboardingPersistenceFixture fixture)
{
    [Test]
    public async Task GivenNewUser_WhenQueryingOnboardingState_StateExists()
    {
        (string externalId, string email, string displayName) = CreateProvisioningInput();

        using IServiceScope scope = fixture.Services.CreateScope();
        UserProvisioningService provisioningService = scope.ServiceProvider.GetRequiredService<UserProvisioningService>();
        await provisioningService.ProvisionOrUpdateAsync(externalId, email, displayName, TestContext.Current!.Execution.CancellationToken);

        using IServiceScope readScope = fixture.Services.CreateScope();
        MenloDbContext db = readScope.ServiceProvider.GetRequiredService<MenloDbContext>();
        ExternalUserId normalizedExternalId = new(externalId);
        User user = await db.Users
            .AsNoTracking()
            .SingleAsync(candidate => candidate.ExternalId == normalizedExternalId, TestContext.Current!.Execution.CancellationToken);
        OnboardingState? onboardingState = await db.OnboardingStates
            .AsNoTracking()
            .FirstOrDefaultAsync(state => state.UserId == user.Id, TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveProvisionedState(onboardingState, user.Id);
    }

    [Test]
    public async Task GivenOnboardingState_WhenCompleteTaskCalled_StateIsUpdated()
    {
        (string externalId, string email, string displayName) = CreateProvisioningInput();

        using IServiceScope scope = fixture.Services.CreateScope();
        UserProvisioningService provisioningService = scope.ServiceProvider.GetRequiredService<UserProvisioningService>();
        await provisioningService.ProvisionOrUpdateAsync(externalId, email, displayName, TestContext.Current!.Execution.CancellationToken);

        MenloDbContext db = scope.ServiceProvider.GetRequiredService<MenloDbContext>();
        ExternalUserId normalizedExternalId = new(externalId);
        User user = await db.Users
            .SingleAsync(candidate => candidate.ExternalId == normalizedExternalId, TestContext.Current!.Execution.CancellationToken);
        OnboardingState onboardingState = await db.OnboardingStates
            .SingleAsync(state => state.UserId == user.Id, TestContext.Current!.Execution.CancellationToken);

        onboardingState.CompleteTask(
            OnboardingTaskType.SelectedHousehold,
            new Dictionary<string, object>
            {
                ["householdId"] = Guid.NewGuid().ToString()
            });
        await db.SaveChangesAsync(TestContext.Current!.Execution.CancellationToken);

        OnboardingState persistedState = await db.OnboardingStates
            .AsNoTracking()
            .SingleAsync(state => state.UserId == user.Id, TestContext.Current!.Execution.CancellationToken);

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
