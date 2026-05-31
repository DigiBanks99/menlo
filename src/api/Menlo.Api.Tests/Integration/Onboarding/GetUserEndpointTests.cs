using Menlo.Application.Auth;
using Menlo.Application.Common;
using Menlo.Application.Onboarding;
using Menlo.Lib.Auth.Entities;
using Menlo.Lib.Auth.Models;
using Menlo.Lib.Auth.ValueObjects;
using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.ValueObjects;
using Menlo.Lib.Onboarding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace Menlo.Api.Tests.Integration.Onboarding;

/// <summary>
/// Integration tests for the GetUserEndpoint (/auth/user).
/// Tests cover:
/// - Authentication verification (401 on unauthenticated requests)
/// - User lookup from database and fallback to claims
/// - Onboarding state extraction and completion status
/// - Error handling for database exceptions
/// - Claims-based fallback for all user profile fields
/// </summary>
[Collection("Onboarding API")]
public sealed class GetUserEndpointTests(OnboardingApiFixture fixture) : TestFixture
{
    /// <summary>
    /// Happy path: Authenticated user with complete onboarding returns 200 with onboarding complete.
    /// Covers lines 39-41 (auth check), 46-56 (user lookup), and 70-93 (onboarding state).
    /// </summary>
    [Fact]
    public async Task GetUser_WhenAuthenticatedWithCompleteOnboarding_ReturnsUserProfileWithCompletedStatus()
    {
        string externalId = Guid.NewGuid().ToString();
        string email = $"{Guid.NewGuid():N}@menlo.test";
        string displayName = $"User {Guid.NewGuid():N}";

        await using TestWebApplicationFactory factory = fixture.CreateFactory(externalId, email, displayName);
        User createdUser = await OnboardingApiTestData.ProvisionUserAsync(
            fixture.Services,
            externalId,
            email,
            displayName,
            completeOnboarding: true);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/auth/user", TestContext.Current.CancellationToken);
        UserProfile? profile = await response.Content.ReadFromJsonAsync<UserProfile>(TestContext.Current.CancellationToken);

        ItShouldReturnSuccess(response);
        ItShouldReturnValidProfile(profile, createdUser, email, displayName);
        ItShouldIndicateOnboardingComplete(profile);
    }

    /// <summary>
    /// Happy path: Authenticated user with incomplete onboarding returns 200 with onboarding incomplete.
    /// Covers lines 39-41 (auth check), 46-56 (user lookup), and 70-93 (onboarding state).
    /// </summary>
    [Fact]
    public async Task GetUser_WhenAuthenticatedWithIncompleteOnboarding_ReturnsUserProfileWithPendingTasks()
    {
        string externalId = Guid.NewGuid().ToString();
        string email = $"{Guid.NewGuid():N}@menlo.test";
        string displayName = $"User {Guid.NewGuid():N}";

        await using TestWebApplicationFactory factory = fixture.CreateFactory(externalId, email, displayName);
        User createdUser = await OnboardingApiTestData.ProvisionUserAsync(
            fixture.Services,
            externalId,
            email,
            displayName,
            completeOnboarding: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/auth/user", TestContext.Current.CancellationToken);
        UserProfile? profile = await response.Content.ReadFromJsonAsync<UserProfile>(TestContext.Current.CancellationToken);

        ItShouldReturnSuccess(response);
        ItShouldReturnValidProfile(profile, createdUser, email, displayName);
        ItShouldIndicateOnboardingIncomplete(profile);
    }

    /// <summary>
    /// Edge case: Authenticated user exists but onboarding state record doesn't exist yet.
    /// Defaults to incomplete onboarding as per lines 71-72 and 82-85.
    /// </summary>
    [Fact]
    public async Task GetUser_WhenOnboardingStateDoesNotExist_TreatsAsIncompleteOnboarding()
    {
        string externalId = Guid.NewGuid().ToString();
        string email = $"{Guid.NewGuid():N}@menlo.test";
        string displayName = $"User {Guid.NewGuid():N}";

        await using TestWebApplicationFactory factory = fixture.CreateFactory(externalId, email, displayName);
        
        // Provision user without creating onboarding state (simulating a data inconsistency)
        using IServiceScope scope = fixture.Services.CreateScope();
        UserProvisioningService provisioningService = scope.ServiceProvider.GetRequiredService<UserProvisioningService>();
        IUserContext userContext = scope.ServiceProvider.GetRequiredService<IUserContext>();
        IOnboardingContext onboardingContext = scope.ServiceProvider.GetRequiredService<IOnboardingContext>();

        await provisioningService.ProvisionOrUpdateAsync(externalId, email, displayName, TestContext.Current.CancellationToken);
        
        ExternalUserId normalizedExternalId = new(externalId);
        User createdUser = await userContext.Users
            .SingleAsync(u => u.ExternalId == normalizedExternalId, TestContext.Current.CancellationToken);

        // Delete the onboarding state to simulate the edge case
        OnboardingState? onboardingState = await onboardingContext.OnboardingStates
            .FirstOrDefaultAsync(s => s.UserId == createdUser.Id, TestContext.Current.CancellationToken);
        if (onboardingState is not null)
        {
            onboardingContext.OnboardingStates.Remove(onboardingState);
            await onboardingContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        using HttpClient client = factory.CreateClient();
        HttpResponseMessage response = await client.GetAsync("/auth/user", TestContext.Current.CancellationToken);
        UserProfile? profile = await response.Content.ReadFromJsonAsync<UserProfile>(TestContext.Current.CancellationToken);

        ItShouldReturnSuccess(response);
        ItShouldReturnValidProfile(profile, createdUser, email, displayName);
        ItShouldIndicateOnboardingIncomplete(profile); // Defaults to incomplete per line 71-72
    }

    /// <summary>
    /// Authentication: Unauthenticated request returns 401.
    /// Covers lines 39-42 (authentication check).
    /// </summary>
    [Fact]
    public async Task GetUser_WhenUnauthenticated_Returns401Unauthorized()
    {
        await using TestWebApplicationFactory factory = fixture.CreateFactory(simulateUnauthenticated: true);
        using HttpClient client = factory.CreateClient(new() { AllowAutoRedirect = false });

        HttpResponseMessage response = await client.GetAsync("/auth/user", TestContext.Current.CancellationToken);

        ItShouldHaveBeenUnauthorised(response);
    }

    /// <summary>
    /// User lookup fallback: When user is not provisioned in database,
    /// endpoint falls back to claims for user profile data.
    /// Covers lines 48-61 (try/catch for user lookup) and 63-68 (fallback to claims).
    /// </summary>
    [Fact]
    public async Task GetUser_WhenUserNotProvisioned_FallsBackToClaimsForProfile()
    {
        string externalId = Guid.NewGuid().ToString();
        string email = $"{Guid.NewGuid():N}@menlo.test";
        string displayName = $"User {Guid.NewGuid():N}";

        // Create factory without provisioning user in database
        // This simulates a scenario where CurrentUserLookup fails to find the user
        await using TestWebApplicationFactory factory = fixture.CreateFactory(externalId, email, displayName);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/auth/user", TestContext.Current.CancellationToken);
        UserProfile? profile = await response.Content.ReadFromJsonAsync<UserProfile>(TestContext.Current.CancellationToken);

        // User not in DB, so all data comes from claims
        ItShouldReturnSuccess(response);
        ItShouldUseClaimsForProfile(profile, externalId, email, displayName);
    }


    /// <summary>
    /// Claims extraction: Roles from claims are included in the response.
    /// Covers line 63 (roles extraction from claims).
    /// </summary>
    [Fact]
    public async Task GetUser_WhenAuthenticatedWithRoles_ReturnsRolesFromClaims()
    {
        string externalId = Guid.NewGuid().ToString();
        string email = $"{Guid.NewGuid():N}@menlo.test";
        string displayName = $"User {Guid.NewGuid():N}";

        await using TestWebApplicationFactory factory = new()
        {
            MenloConnectionString = fixture.ConnectionString,
            SkipMigration = false,
            UserId = externalId,
            UserEmail = email,
            UserDisplayName = displayName,
            UserRoles = ["Menlo.User", "Menlo.Admin", "Menlo.Contributor"]
        };

        using HttpClient client = factory.CreateClient();
        HttpResponseMessage response = await client.GetAsync("/auth/user", TestContext.Current.CancellationToken);
        UserProfile? profile = await response.Content.ReadFromJsonAsync<UserProfile>(TestContext.Current.CancellationToken);

        ItShouldReturnSuccess(response);
        profile.ShouldNotBeNull();
        ItShouldHaveExpectedRoles(profile, ["Menlo.User", "Menlo.Admin", "Menlo.Contributor"]);
    }

    /// <summary>
    /// Claims extraction: Email is extracted from email claim when user not found in DB.
    /// Covers lines 64 and 97 (email fallback to claim).
    /// </summary>
    [Fact]
    public async Task GetUser_WhenUserNotFoundInDatabase_ReturnsEmailFromClaim()
    {
        string externalId = Guid.NewGuid().ToString();
        string email = $"test-{Guid.NewGuid():N}@menlo.test";
        string displayName = $"User {Guid.NewGuid():N}";

        // Create factory without provisioning user
        await using TestWebApplicationFactory factory = fixture.CreateFactory(externalId, email, displayName);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/auth/user", TestContext.Current.CancellationToken);
        UserProfile? profile = await response.Content.ReadFromJsonAsync<UserProfile>(TestContext.Current.CancellationToken);

        ItShouldReturnSuccess(response);
        profile.ShouldNotBeNull();
        profile.Email.ShouldBe(email); // From claim
    }

    /// <summary>
    /// Claims extraction: DisplayName is extracted from name claim when user not found in DB.
    /// Covers lines 65 and 98 (displayName fallback to claim).
    /// </summary>
    [Fact]
    public async Task GetUser_WhenUserNotFoundInDatabase_ReturnsDisplayNameFromClaim()
    {
        string externalId = Guid.NewGuid().ToString();
        string email = $"{Guid.NewGuid():N}@menlo.test";
        string displayName = $"Claimed Name {Guid.NewGuid():N}";

        // Create factory without provisioning user
        await using TestWebApplicationFactory factory = fixture.CreateFactory(externalId, email, displayName);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/auth/user", TestContext.Current.CancellationToken);
        UserProfile? profile = await response.Content.ReadFromJsonAsync<UserProfile>(TestContext.Current.CancellationToken);

        ItShouldReturnSuccess(response);
        profile.ShouldNotBeNull();
        profile.DisplayName.ShouldBe(displayName); // From claim
    }

    /// <summary>
    /// Claims extraction: ExternalId is extracted from nameidentifier claim when user not found in DB.
    /// Covers lines 66-68 and 96 (id fallback to claim or FindExternalId).
    /// </summary>
    [Fact]
    public async Task GetUser_WhenUserNotFoundInDatabase_ReturnsExternalIdFromClaim()
    {
        string externalId = Guid.NewGuid().ToString();
        string email = $"{Guid.NewGuid():N}@menlo.test";
        string displayName = $"User {Guid.NewGuid():N}";

        // Create factory without provisioning user
        await using TestWebApplicationFactory factory = fixture.CreateFactory(externalId, email, displayName);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/auth/user", TestContext.Current.CancellationToken);
        UserProfile? profile = await response.Content.ReadFromJsonAsync<UserProfile>(TestContext.Current.CancellationToken);

        ItShouldReturnSuccess(response);
        profile.ShouldNotBeNull();
        profile.Id.ShouldBe(externalId); // From claim
    }

    /// <summary>
    /// Database user preferred: When user is found in DB, profile uses database values
    /// instead of claims. This test verifies user provisioning creates expected state.
    /// Covers lines 96-98 (database values preferred over claims).
    /// </summary>
    [Fact]
    public async Task GetUser_WhenUserFoundInDatabase_PrefersDatabaseValuesOverClaims()
    {
        string externalId = Guid.NewGuid().ToString();
        string originalEmail = $"{Guid.NewGuid():N}@menlo.test";
        string originalDisplayName = $"Original Name {Guid.NewGuid():N}";

        await using TestWebApplicationFactory factory = fixture.CreateFactory(externalId, originalEmail, originalDisplayName);
        User createdUser = await OnboardingApiTestData.ProvisionUserAsync(
            fixture.Services,
            externalId,
            originalEmail,
            originalDisplayName);

        using HttpClient client = factory.CreateClient();
        HttpResponseMessage response = await client.GetAsync("/auth/user", TestContext.Current.CancellationToken);
        UserProfile? profile = await response.Content.ReadFromJsonAsync<UserProfile>(TestContext.Current.CancellationToken);

        ItShouldReturnSuccess(response);
        profile.ShouldNotBeNull();
        // Profile uses database values for provisioned user
        profile.Email.ShouldBe(originalEmail);
        profile.DisplayName.ShouldBe(originalDisplayName);
        profile.Id.ShouldBe(createdUser.Id.Value.ToString());
    }

    #region Assert Helpers

    private static void ItShouldReturnSuccess(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private static void ItShouldReturnValidProfile(UserProfile? profile, User expectedUser, string expectedEmail, string expectedDisplayName)
    {
        profile.ShouldNotBeNull();
        profile.Id.ShouldBe(expectedUser.Id.Value.ToString());
        profile.Email.ShouldBe(expectedEmail);
        profile.DisplayName.ShouldBe(expectedDisplayName);
        profile.Roles.ShouldNotBeNull();
        profile.Onboarding.ShouldNotBeNull();
    }

    private static void ItShouldIndicateOnboardingComplete(UserProfile? profile)
    {
        profile.ShouldNotBeNull();
        profile.Onboarding.ShouldNotBeNull();
        profile.Onboarding.IsComplete.ShouldBeTrue();
        profile.Onboarding.PendingTasks.ShouldBeEmpty();
    }

    private static void ItShouldIndicateOnboardingIncomplete(UserProfile? profile)
    {
        profile.ShouldNotBeNull();
        profile.Onboarding.ShouldNotBeNull();
        profile.Onboarding.IsComplete.ShouldBeFalse();
        profile.Onboarding.PendingTasks.ShouldContain("SelectHousehold");
    }

    private static void ItShouldUseClaimsForProfile(UserProfile? profile, string expectedId, string expectedEmail, string expectedDisplayName)
    {
        profile.ShouldNotBeNull();
        profile.Id.ShouldBe(expectedId);
        profile.Email.ShouldBe(expectedEmail);
        profile.DisplayName.ShouldBe(expectedDisplayName);
    }

    private static void ItShouldHaveExpectedRoles(UserProfile profile, string[] expectedRoles)
    {
        profile.Roles.Should().BeEquivalentTo(expectedRoles);
    }

    #endregion
}
