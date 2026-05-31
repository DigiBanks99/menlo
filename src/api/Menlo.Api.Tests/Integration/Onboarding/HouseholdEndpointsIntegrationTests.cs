using Menlo.Api.Endpoints.Onboarding;
using Menlo.Lib.Auth.Entities;
using System.Net;
using System.Net.Http.Json;

namespace Menlo.Api.Tests.Integration.Onboarding;

[Collection("Onboarding API")]
public sealed class HouseholdEndpointsIntegrationTests(OnboardingApiFixture fixture) : TestFixture
{
    [Fact]
    public async Task GivenAuthenticatedUser_WhenListingHouseholds_Returns200()
    {
        await OnboardingApiTestData.CreateHouseholdAsync(fixture.Services, $"Household {Guid.NewGuid():N}");
        await OnboardingApiTestData.CreateHouseholdAsync(fixture.Services, $"Household {Guid.NewGuid():N}");

        await using TestWebApplicationFactory factory = fixture.CreateFactory();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/households", TestContext.Current.CancellationToken);
        HouseholdListResponse? body = await response.Content.ReadFromJsonAsync<HouseholdListResponse>(TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body.ShouldNotBeNull();
        body.Households.Count.ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task GivenUnauthenticatedRequest_WhenListingHouseholds_Returns401()
    {
        await using TestWebApplicationFactory factory = fixture.CreateFactory(simulateUnauthenticated: true);
        using HttpClient client = factory.CreateClient(new() { AllowAutoRedirect = false });

        HttpResponseMessage response = await client.GetAsync("/api/households", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GivenAuthenticatedUser_WhenCreatingHousehold_Returns201AndMarksOnboardingComplete()
    {
        string externalId = Guid.NewGuid().ToString();
        string email = $"{Guid.NewGuid():N}@menlo.test";
        string displayName = $"User {Guid.NewGuid():N}";
        string householdName = $"My Family {Guid.NewGuid():N}";

        await using TestWebApplicationFactory factory = fixture.CreateFactory(externalId, email, displayName);
        User createdUser = await OnboardingApiTestData.ProvisionUserAsync(factory.Services, externalId, email, displayName);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current.CancellationToken);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/households",
            new CreateHouseholdRequest(householdName),
            TestContext.Current.CancellationToken);
        HouseholdSummaryResponse? body = await response.Content.ReadFromJsonAsync<HouseholdSummaryResponse>(TestContext.Current.CancellationToken);

        User persistedUser = await OnboardingApiTestData.GetUserAsync(factory.Services, createdUser.Id);
        var onboardingState = await OnboardingApiTestData.GetOnboardingStateAsync(factory.Services, createdUser.Id);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        body.ShouldNotBeNull();
        body.Name.ShouldBe(householdName);
        persistedUser.HouseholdId.ShouldNotBeNull();
        onboardingState.HasSelectedHousehold.ShouldBeTrue();
    }

    [Fact]
    public async Task GivenDuplicateHouseholdName_WhenCreating_Returns409()
    {
        string externalId = Guid.NewGuid().ToString();
        string email = $"{Guid.NewGuid():N}@menlo.test";
        string displayName = $"User {Guid.NewGuid():N}";
        string householdName = $"Duplicate {Guid.NewGuid():N}";

        await using TestWebApplicationFactory factory = fixture.CreateFactory(externalId, email, displayName);
        _ = await OnboardingApiTestData.ProvisionUserAsync(fixture.Services, externalId, email, displayName);
        _ = await OnboardingApiTestData.CreateHouseholdAsync(fixture.Services, householdName);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current.CancellationToken);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/households",
            new CreateHouseholdRequest(householdName),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GivenAuthenticatedUser_WhenJoiningExistingHousehold_Returns204AndMarksOnboardingComplete()
    {
        string externalId = Guid.NewGuid().ToString();
        string email = $"{Guid.NewGuid():N}@menlo.test";
        string displayName = $"User {Guid.NewGuid():N}";
        string householdName = $"Joinable {Guid.NewGuid():N}";

        await using TestWebApplicationFactory factory = fixture.CreateFactory(externalId, email, displayName);
        User createdUser = await OnboardingApiTestData.ProvisionUserAsync(fixture.Services, externalId, email, displayName);
        Household household = await OnboardingApiTestData.CreateHouseholdAsync(fixture.Services, householdName);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current.CancellationToken);

        HttpResponseMessage response = await client.PostAsync($"/api/households/{household.Id.Value}/join", null, TestContext.Current.CancellationToken);

        User persistedUser = await OnboardingApiTestData.GetUserAsync(factory.Services, createdUser.Id);
        var onboardingState = await OnboardingApiTestData.GetOnboardingStateAsync(factory.Services, createdUser.Id);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        persistedUser.HouseholdId.ShouldBe(household.Id);
        onboardingState.HasSelectedHousehold.ShouldBeTrue();
    }

    [Fact]
    public async Task GivenInvalidHouseholdId_WhenJoining_Returns404()
    {
        string externalId = Guid.NewGuid().ToString();
        string email = $"{Guid.NewGuid():N}@menlo.test";
        string displayName = $"User {Guid.NewGuid():N}";

        await using TestWebApplicationFactory factory = fixture.CreateFactory(externalId, email, displayName);
        _ = await OnboardingApiTestData.ProvisionUserAsync(fixture.Services, externalId, email, displayName);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(cancellationToken: TestContext.Current.CancellationToken);

        HttpResponseMessage response = await client.PostAsync($"/api/households/{Guid.NewGuid()}/join", null, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
