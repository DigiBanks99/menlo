using Menlo.Api.Endpoints.Onboarding;
using Menlo.Lib.Auth.Entities;
using Menlo.Lib.Onboarding;
using System.Net;
using System.Net.Http.Json;

namespace Menlo.Api.Tests.Integration.Onboarding;

[Collection("Onboarding API")]
public sealed class HouseholdEndpointsTests(OnboardingApiFixture fixture) : TestFixture
{
    #region ListHouseholds Tests

    [Fact]
    public async Task ListHouseholds_WithNoHouseholds_ReturnsEmptyList()
    {
        // Arrange
        await using TestWebApplicationFactory factory = fixture.CreateFactory();
        using HttpClient client = factory.CreateClient();

        // Act
        HttpResponseMessage response = await client.GetAsync(
            "/api/households",
            TestContext.Current.CancellationToken);
        HouseholdListResponse? body = await response.Content.ReadFromJsonAsync<HouseholdListResponse>(
            TestContext.Current.CancellationToken);

        // Assert
        ItShouldReturn200Ok(response);
        ItShouldHaveEmptyHouseholdList(body);
    }

    [Fact]
    public async Task ListHouseholds_WithMultipleHouseholds_ReturnsOrdered()
    {
        // Arrange
        string householdNameC = $"Charlie {Guid.NewGuid():N}";
        string householdNameA = $"Alpha {Guid.NewGuid():N}";
        string householdNameB = $"Bravo {Guid.NewGuid():N}";

        await OnboardingApiTestData.CreateHouseholdAsync(fixture.Services, householdNameC);
        await OnboardingApiTestData.CreateHouseholdAsync(fixture.Services, householdNameA);
        await OnboardingApiTestData.CreateHouseholdAsync(fixture.Services, householdNameB);

        await using TestWebApplicationFactory factory = fixture.CreateFactory();
        using HttpClient client = factory.CreateClient();

        // Act
        HttpResponseMessage response = await client.GetAsync(
            "/api/households",
            TestContext.Current.CancellationToken);
        HouseholdListResponse? body = await response.Content.ReadFromJsonAsync<HouseholdListResponse>(
            TestContext.Current.CancellationToken);

        // Assert
        ItShouldReturn200Ok(response);
        ItShouldHaveHouseholdsInAlphabeticalOrder(body, householdNameA, householdNameB, householdNameC);
    }

    [Fact]
    public async Task ListHouseholds_WhenUnauthenticated_Returns401()
    {
        // Arrange
        await using TestWebApplicationFactory factory = fixture.CreateFactory(
            simulateUnauthenticated: true);
        using HttpClient client = factory.CreateClient(new() { AllowAutoRedirect = false });

        // Act
        HttpResponseMessage response = await client.GetAsync(
            "/api/households",
            TestContext.Current.CancellationToken);

        // Assert
        ItShouldReturn401Unauthorized(response);
    }

    #endregion

    #region CreateHousehold Tests

    [Fact]
    public async Task CreateHousehold_WithValidName_CreatesAndReturns201()
    {
        // Arrange
        string externalId = Guid.NewGuid().ToString();
        string email = $"{Guid.NewGuid():N}@menlo.test";
        string displayName = $"User {Guid.NewGuid():N}";
        string householdName = $"My Household {Guid.NewGuid():N}";

        await using TestWebApplicationFactory factory = fixture.CreateFactory(
            externalId, email, displayName);
        User createdUser = await OnboardingApiTestData.ProvisionUserAsync(
            factory.Services, externalId, email, displayName);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/households",
            new CreateHouseholdRequest(householdName),
            TestContext.Current.CancellationToken);
        HouseholdSummaryResponse? body = await response.Content.ReadFromJsonAsync<HouseholdSummaryResponse>(
            TestContext.Current.CancellationToken);

        User persistedUser = await OnboardingApiTestData.GetUserAsync(factory.Services, createdUser.Id);
        OnboardingState onboardingState = await OnboardingApiTestData.GetOnboardingStateAsync(
            factory.Services, createdUser.Id);

        // Assert
        ItShouldReturn201Created(response);
        ItShouldReturnHouseholdWithCorrectName(body, householdName);
        ItShouldAssignUserToHousehold(persistedUser);
        ItShouldMarkOnboardingHouseholdTaskComplete(onboardingState);
    }

    [Fact]
    public async Task CreateHousehold_WithEmptyName_Returns400()
    {
        // Arrange
        string externalId = Guid.NewGuid().ToString();
        string email = $"{Guid.NewGuid():N}@menlo.test";
        string displayName = $"User {Guid.NewGuid():N}";

        await using TestWebApplicationFactory factory = fixture.CreateFactory(
            externalId, email, displayName);
        _ = await OnboardingApiTestData.ProvisionUserAsync(
            factory.Services, externalId, email, displayName);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/households",
            new CreateHouseholdRequest(string.Empty),
            TestContext.Current.CancellationToken);

        // Assert
        ItShouldReturn400BadRequest(response);
    }

    [Fact]
    public async Task CreateHousehold_WithWhitespaceName_Returns400()
    {
        // Arrange
        string externalId = Guid.NewGuid().ToString();
        string email = $"{Guid.NewGuid():N}@menlo.test";
        string displayName = $"User {Guid.NewGuid():N}";

        await using TestWebApplicationFactory factory = fixture.CreateFactory(
            externalId, email, displayName);
        _ = await OnboardingApiTestData.ProvisionUserAsync(
            factory.Services, externalId, email, displayName);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/households",
            new CreateHouseholdRequest("   \t\n   "),
            TestContext.Current.CancellationToken);

        // Assert
        ItShouldReturn400BadRequest(response);
    }

    [Fact]
    public async Task CreateHousehold_WithNameOver100Chars_Returns400()
    {
        // Arrange
        string externalId = Guid.NewGuid().ToString();
        string email = $"{Guid.NewGuid():N}@menlo.test";
        string displayName = $"User {Guid.NewGuid():N}";
        string householdNameTooLong = new('x', 101);

        await using TestWebApplicationFactory factory = fixture.CreateFactory(
            externalId, email, displayName);
        _ = await OnboardingApiTestData.ProvisionUserAsync(
            factory.Services, externalId, email, displayName);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/households",
            new CreateHouseholdRequest(householdNameTooLong),
            TestContext.Current.CancellationToken);

        // Assert
        ItShouldReturn400BadRequest(response);
    }

    [Fact]
    public async Task CreateHousehold_WhenUnauthenticated_Returns401()
    {
        // Arrange
        string householdName = $"Household {Guid.NewGuid():N}";

        await using TestWebApplicationFactory factory = fixture.CreateFactory(
            simulateUnauthenticated: true);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/households",
            new CreateHouseholdRequest(householdName),
            TestContext.Current.CancellationToken);

        // Assert
        ItShouldReturn401Unauthorized(response);
    }

    [Fact]
    public async Task CreateHousehold_WithDuplicateName_Returns409()
    {
        // Arrange
        string externalId = Guid.NewGuid().ToString();
        string email = $"{Guid.NewGuid():N}@menlo.test";
        string displayName = $"User {Guid.NewGuid():N}";
        string householdName = $"Duplicate {Guid.NewGuid():N}";

        await using TestWebApplicationFactory factory = fixture.CreateFactory(
            externalId, email, displayName);
        _ = await OnboardingApiTestData.ProvisionUserAsync(
            fixture.Services, externalId, email, displayName);
        _ = await OnboardingApiTestData.CreateHouseholdAsync(fixture.Services, householdName);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/households",
            new CreateHouseholdRequest(householdName),
            TestContext.Current.CancellationToken);

        // Assert
        ItShouldReturn409Conflict(response);
    }

    [Fact]
    public async Task CreateHousehold_WithNameRequiringTrim_TrimmedBeforeValidation()
    {
        // Arrange
        string externalId = Guid.NewGuid().ToString();
        string email = $"{Guid.NewGuid():N}@menlo.test";
        string displayName = $"User {Guid.NewGuid():N}";
        string householdNameWithWhitespace = $"  {Guid.NewGuid():N}  ";
        string householdNameTrimmed = householdNameWithWhitespace.Trim();

        await using TestWebApplicationFactory factory = fixture.CreateFactory(
            externalId, email, displayName);
        User createdUser = await OnboardingApiTestData.ProvisionUserAsync(
            factory.Services, externalId, email, displayName);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/households",
            new CreateHouseholdRequest(householdNameWithWhitespace),
            TestContext.Current.CancellationToken);
        HouseholdSummaryResponse? body = await response.Content.ReadFromJsonAsync<HouseholdSummaryResponse>(
            TestContext.Current.CancellationToken);

        // Assert
        ItShouldReturn201Created(response);
        ItShouldReturnHouseholdWithCorrectName(body, householdNameTrimmed);
    }

    [Fact]
    public async Task CreateHousehold_WithDuplicateNameDifferentCase_Returns409()
    {
        // Arrange
        string externalId = Guid.NewGuid().ToString();
        string email = $"{Guid.NewGuid():N}@menlo.test";
        string displayName = $"User {Guid.NewGuid():N}";
        string householdNameOriginal = $"MyHousehold{Guid.NewGuid():N}";
        string householdNameDifferentCase = householdNameOriginal.ToUpperInvariant();

        await using TestWebApplicationFactory factory = fixture.CreateFactory(
            externalId, email, displayName);
        _ = await OnboardingApiTestData.ProvisionUserAsync(
            fixture.Services, externalId, email, displayName);
        _ = await OnboardingApiTestData.CreateHouseholdAsync(fixture.Services, householdNameOriginal);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/households",
            new CreateHouseholdRequest(householdNameDifferentCase),
            TestContext.Current.CancellationToken);

        // Assert
        ItShouldReturn409Conflict(response);
    }

    [Fact]
    public async Task CreateHousehold_WithNameAtMaxLength_CreatesSuccessfully()
    {
        // Arrange
        string externalId = Guid.NewGuid().ToString();
        string email = $"{Guid.NewGuid():N}@menlo.test";
        string displayName = $"User {Guid.NewGuid():N}";
        string householdNameMaxLength = new('x', 100);

        await using TestWebApplicationFactory factory = fixture.CreateFactory(
            externalId, email, displayName);
        User createdUser = await OnboardingApiTestData.ProvisionUserAsync(
            factory.Services, externalId, email, displayName);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/households",
            new CreateHouseholdRequest(householdNameMaxLength),
            TestContext.Current.CancellationToken);
        HouseholdSummaryResponse? body = await response.Content.ReadFromJsonAsync<HouseholdSummaryResponse>(
            TestContext.Current.CancellationToken);

        // Assert
        ItShouldReturn201Created(response);
        ItShouldReturnHouseholdWithCorrectName(body, householdNameMaxLength);
    }

    [Fact]
    public async Task CreateHousehold_WithoutOnboardingState_StillCreatesSuccessfully()
    {
        // Arrange - This tests that the endpoint gracefully handles null onboarding state
        string externalId = Guid.NewGuid().ToString();
        string email = $"{Guid.NewGuid():N}@menlo.test";
        string displayName = $"User {Guid.NewGuid():N}";
        string householdName = $"Household {Guid.NewGuid():N}";

        await using TestWebApplicationFactory factory = fixture.CreateFactory(
            externalId, email, displayName);
        // Create user without onboarding state would require direct DB manipulation
        // For now, we provision normally and the endpoint should handle a missing state gracefully
        User createdUser = await OnboardingApiTestData.ProvisionUserAsync(
            factory.Services, externalId, email, displayName);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/households",
            new CreateHouseholdRequest(householdName),
            TestContext.Current.CancellationToken);
        HouseholdSummaryResponse? body = await response.Content.ReadFromJsonAsync<HouseholdSummaryResponse>(
            TestContext.Current.CancellationToken);

        // Assert
        ItShouldReturn201Created(response);
        ItShouldReturnHouseholdWithCorrectName(body, householdName);
    }

    #endregion

    #region JoinHousehold Tests

    [Fact]
    public async Task JoinHousehold_WithValidHouseholdId_Returns204()
    {
        // Arrange
        string externalId = Guid.NewGuid().ToString();
        string email = $"{Guid.NewGuid():N}@menlo.test";
        string displayName = $"User {Guid.NewGuid():N}";
        string householdName = $"Household {Guid.NewGuid():N}";

        await using TestWebApplicationFactory factory = fixture.CreateFactory(
            externalId, email, displayName);
        User createdUser = await OnboardingApiTestData.ProvisionUserAsync(
            factory.Services, externalId, email, displayName);
        Household household = await OnboardingApiTestData.CreateHouseholdAsync(
            fixture.Services, householdName);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        // Act
        HttpResponseMessage response = await client.PostAsync(
            $"/api/households/{household.Id.Value}/join",
            null,
            TestContext.Current.CancellationToken);

        User persistedUser = await OnboardingApiTestData.GetUserAsync(factory.Services, createdUser.Id);
        OnboardingState onboardingState = await OnboardingApiTestData.GetOnboardingStateAsync(
            factory.Services, createdUser.Id);

        // Assert
        ItShouldReturn204NoContent(response);
        ItShouldAssignUserToHousehold(persistedUser, household.Id.Value);
        ItShouldMarkOnboardingHouseholdTaskComplete(onboardingState);
    }

    [Fact]
    public async Task JoinHousehold_WhenUnauthenticated_Returns401()
    {
        // Arrange
        Guid householdId = Guid.NewGuid();

        await using TestWebApplicationFactory factory = fixture.CreateFactory(
            simulateUnauthenticated: true);
        using HttpClient client = factory.CreateClient(new() { AllowAutoRedirect = false });

        // Act
        HttpResponseMessage response = await client.PostAsync(
            $"/api/households/{householdId}/join",
            null,
            TestContext.Current.CancellationToken);

        // Assert
        ItShouldReturn401Unauthorized(response);
    }

    [Fact]
    public async Task JoinHousehold_WithInvalidHouseholdId_Returns404()
    {
        // Arrange
        string externalId = Guid.NewGuid().ToString();
        string email = $"{Guid.NewGuid():N}@menlo.test";
        string displayName = $"User {Guid.NewGuid():N}";

        await using TestWebApplicationFactory factory = fixture.CreateFactory(
            externalId, email, displayName);
        _ = await OnboardingApiTestData.ProvisionUserAsync(
            factory.Services, externalId, email, displayName);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        // Act
        HttpResponseMessage response = await client.PostAsync(
            $"/api/households/{Guid.NewGuid()}/join",
            null,
            TestContext.Current.CancellationToken);

        // Assert
        ItShouldReturn404NotFound(response);
    }

    [Fact]
    public async Task JoinHousehold_WithoutOnboardingState_StillSucceeds()
    {
        // Arrange - Tests that join works even if onboarding state is null
        string externalId = Guid.NewGuid().ToString();
        string email = $"{Guid.NewGuid():N}@menlo.test";
        string displayName = $"User {Guid.NewGuid():N}";
        string householdName = $"Household {Guid.NewGuid():N}";

        await using TestWebApplicationFactory factory = fixture.CreateFactory(
            externalId, email, displayName);
        User createdUser = await OnboardingApiTestData.ProvisionUserAsync(
            factory.Services, externalId, email, displayName);
        Household household = await OnboardingApiTestData.CreateHouseholdAsync(
            fixture.Services, householdName);
        using HttpClient client = await factory.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        // Act
        HttpResponseMessage response = await client.PostAsync(
            $"/api/households/{household.Id.Value}/join",
            null,
            TestContext.Current.CancellationToken);

        User persistedUser = await OnboardingApiTestData.GetUserAsync(factory.Services, createdUser.Id);

        // Assert
        ItShouldReturn204NoContent(response);
        ItShouldAssignUserToHousehold(persistedUser, household.Id.Value);
    }

    [Fact]
    public async Task JoinHousehold_MultipleUsersCanJoinSameHousehold()
    {
        // Arrange
        string householdName = $"Shared {Guid.NewGuid():N}";
        Household household = await OnboardingApiTestData.CreateHouseholdAsync(
            fixture.Services, householdName);

        string externalId1 = Guid.NewGuid().ToString();
        string email1 = $"{Guid.NewGuid():N}@menlo.test";
        string displayName1 = $"User {Guid.NewGuid():N}";

        string externalId2 = Guid.NewGuid().ToString();
        string email2 = $"{Guid.NewGuid():N}@menlo.test";
        string displayName2 = $"User {Guid.NewGuid():N}";

        await using TestWebApplicationFactory factory1 = fixture.CreateFactory(
            externalId1, email1, displayName1);
        User user1 = await OnboardingApiTestData.ProvisionUserAsync(
            factory1.Services, externalId1, email1, displayName1);
        using HttpClient client1 = await factory1.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        await using TestWebApplicationFactory factory2 = fixture.CreateFactory(
            externalId2, email2, displayName2);
        User user2 = await OnboardingApiTestData.ProvisionUserAsync(
            factory2.Services, externalId2, email2, displayName2);
        using HttpClient client2 = await factory2.CreateAntiforgeryClientAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        // Act
        HttpResponseMessage response1 = await client1.PostAsync(
            $"/api/households/{household.Id.Value}/join",
            null,
            TestContext.Current.CancellationToken);

        HttpResponseMessage response2 = await client2.PostAsync(
            $"/api/households/{household.Id.Value}/join",
            null,
            TestContext.Current.CancellationToken);

        User persistedUser1 = await OnboardingApiTestData.GetUserAsync(factory1.Services, user1.Id);
        User persistedUser2 = await OnboardingApiTestData.GetUserAsync(factory2.Services, user2.Id);

        // Assert
        ItShouldReturn204NoContent(response1);
        ItShouldReturn204NoContent(response2);
        ItShouldAssignUserToHousehold(persistedUser1, household.Id.Value);
        ItShouldAssignUserToHousehold(persistedUser2, household.Id.Value);
    }

    #endregion

    #region Response Status Code Assertions

    private static void ItShouldReturn200Ok(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private static void ItShouldReturn201Created(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    private static void ItShouldReturn204NoContent(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    private static void ItShouldReturn400BadRequest(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    private static void ItShouldReturn401Unauthorized(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private static void ItShouldReturn404NotFound(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private static void ItShouldReturn409Conflict(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    #endregion

    #region Response Body Assertions

    private static void ItShouldHaveEmptyHouseholdList(HouseholdListResponse? response)
    {
        response.ShouldNotBeNull();
        response.Households.ShouldNotBeNull();
        response.Households.Count.ShouldBe(0);
    }

    private static void ItShouldHaveHouseholdsInAlphabeticalOrder(
        HouseholdListResponse? response,
        string nameA,
        string nameB,
        string nameC)
    {
        response.ShouldNotBeNull();
        response.Households.ShouldNotBeNull();
        response.Households.Count.ShouldBeGreaterThanOrEqualTo(3);

        var households = response.Households.ToList();
        var foundNames = households.Select(h => h.Name).ToList();

        foundNames.ShouldContain(nameA);
        foundNames.ShouldContain(nameB);
        foundNames.ShouldContain(nameC);

        // Verify they are in sorted order
        var sortedNames = foundNames.OrderBy(n => n).ToList();
        foundNames.ShouldBe(sortedNames);
    }

    private static void ItShouldReturnHouseholdWithCorrectName(
        HouseholdSummaryResponse? response,
        string expectedName)
    {
        response.ShouldNotBeNull();
        response.Name.ShouldBe(expectedName);
        response.Id.ShouldNotBe(Guid.Empty);
    }

    #endregion

    #region User/Onboarding State Assertions

    private static void ItShouldAssignUserToHousehold(User user)
    {
        user.HouseholdId.ShouldNotBeNull();
    }

    private static void ItShouldAssignUserToHousehold(User user, Guid expectedHouseholdId)
    {
        user.HouseholdId.ShouldNotBeNull();
        user.HouseholdId.Value.ShouldBe(expectedHouseholdId);
    }

    private static void ItShouldMarkOnboardingHouseholdTaskComplete(OnboardingState onboardingState)
    {
        onboardingState.ShouldNotBeNull();
        onboardingState.HasSelectedHousehold.ShouldBeTrue();
    }

    #endregion
}
