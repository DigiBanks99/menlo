using Menlo.Application.Common;
using Menlo.Application.Onboarding;
using Menlo.Lib.Auth.Entities;
using Menlo.Lib.Auth.ValueObjects;
using Menlo.Lib.Onboarding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Menlo.Api.Tests.Integration.Onboarding;

[Collection("Onboarding")]
public sealed class UserProvisioningServiceTests(OnboardingPersistenceFixture fixture)
{
    [Fact]
    public async Task GivenNewExternalUser_WhenProvisioningCalled_UserAndOnboardingStateAreCreated()
    {
        (string externalId, string email, string displayName) = CreateProvisioningInput();

        using IServiceScope scope = fixture.Services.CreateScope();
        UserProvisioningService service = scope.ServiceProvider.GetRequiredService<UserProvisioningService>();

        await service.ProvisionOrUpdateAsync(externalId, email, displayName, TestContext.Current.CancellationToken);

        using IServiceScope readScope = fixture.Services.CreateScope();
        MenloDbContext db = readScope.ServiceProvider.GetRequiredService<MenloDbContext>();
        ExternalUserId persistedExternalId = new(externalId);
        User? user = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.ExternalId == persistedExternalId, TestContext.Current.CancellationToken);
        OnboardingState? onboardingState = await db.OnboardingStates
            .AsNoTracking()
            .FirstOrDefaultAsync(state => state.UserId == user!.Id, TestContext.Current.CancellationToken);

        ItShouldHaveProvisionedUser(user, email, displayName, persistedExternalId);
        ItShouldHaveProvisionedOnboardingState(onboardingState, user!.Id);
    }

    [Fact]
    public async Task GivenExistingExternalUser_WhenProvisioningCalled_NoNewRecordIsCreated()
    {
        (string externalId, string email, string displayName) = CreateProvisioningInput();

        using IServiceScope scope = fixture.Services.CreateScope();
        UserProvisioningService service = scope.ServiceProvider.GetRequiredService<UserProvisioningService>();

        await service.ProvisionOrUpdateAsync(externalId, email, displayName, TestContext.Current.CancellationToken);
        await service.ProvisionOrUpdateAsync(externalId, $"updated-{email}", $"Updated {displayName}", TestContext.Current.CancellationToken);

        using IServiceScope readScope = fixture.Services.CreateScope();
        MenloDbContext db = readScope.ServiceProvider.GetRequiredService<MenloDbContext>();
        ExternalUserId persistedExternalId = new(externalId);
        int userCount = await db.Users.CountAsync(user => user.ExternalId == persistedExternalId, TestContext.Current.CancellationToken);
        User persistedUser = await db.Users
            .AsNoTracking()
            .SingleAsync(user => user.ExternalId == persistedExternalId, TestContext.Current.CancellationToken);
        int onboardingStateCount = await db.OnboardingStates
            .CountAsync(state => state.UserId == persistedUser.Id, TestContext.Current.CancellationToken);

        ItShouldHaveSingleProvisionedRecord(userCount, onboardingStateCount);
        ItShouldRetainOriginalUserDetails(persistedUser, email, displayName);
    }

    [Fact]
    public async Task GivenProvisionedUser_WhenQuerying_OnboardingStateExists()
    {
        (string externalId, string email, string displayName) = CreateProvisioningInput();

        using IServiceScope scope = fixture.Services.CreateScope();
        UserProvisioningService service = scope.ServiceProvider.GetRequiredService<UserProvisioningService>();

        await service.ProvisionOrUpdateAsync(externalId, email, displayName, TestContext.Current.CancellationToken);

        using IServiceScope readScope = fixture.Services.CreateScope();
        MenloDbContext db = readScope.ServiceProvider.GetRequiredService<MenloDbContext>();
        ExternalUserId persistedExternalId = new(externalId);
        User user = await db.Users
            .AsNoTracking()
            .SingleAsync(candidate => candidate.ExternalId == persistedExternalId, TestContext.Current.CancellationToken);
        OnboardingState? onboardingState = await db.OnboardingStates
            .AsNoTracking()
            .FirstOrDefaultAsync(state => state.UserId == user.Id, TestContext.Current.CancellationToken);

        ItShouldHaveProvisionedOnboardingState(onboardingState, user.Id);
    }

    private static (string externalId, string email, string displayName) CreateProvisioningInput()
    {
        string uniqueSuffix = Guid.NewGuid().ToString("N");
        return ($"external-{uniqueSuffix}", $"user-{uniqueSuffix}@menlo.app", $"User {uniqueSuffix}");
    }

    private static void ItShouldHaveProvisionedUser(User? user, string expectedEmail, string expectedDisplayName, ExternalUserId expectedExternalId)
    {
        user.ShouldNotBeNull();
        user.ExternalId.ShouldBe(expectedExternalId);
        user.Email.ShouldBe(expectedEmail);
        user.DisplayName.ShouldBe(expectedDisplayName);
    }

    private static void ItShouldHaveProvisionedOnboardingState(OnboardingState? onboardingState, Menlo.Lib.Common.ValueObjects.UserId expectedUserId)
    {
        onboardingState.ShouldNotBeNull();
        onboardingState.UserId.ShouldBe(expectedUserId);
        onboardingState.CompletedTasks.ShouldBeEmpty();
        onboardingState.IsFullyOnboarded.ShouldBeFalse();
    }

    private static void ItShouldHaveSingleProvisionedRecord(int userCount, int onboardingStateCount)
    {
        userCount.ShouldBe(1);
        onboardingStateCount.ShouldBe(1);
    }

    private static void ItShouldRetainOriginalUserDetails(User user, string expectedEmail, string expectedDisplayName)
    {
        user.Email.ShouldBe(expectedEmail);
        user.DisplayName.ShouldBe(expectedDisplayName);
    }
}
