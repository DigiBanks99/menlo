using Menlo.Lib.Auth.Entities;
using Menlo.Lib.Auth.Models;
using System.Net;
using System.Net.Http.Json;

namespace Menlo.Api.Tests.Integration.Onboarding;

[Collection("Onboarding API")]
public sealed class GetUserEndpointIntegrationTests(OnboardingApiFixture fixture) : TestFixture
{
    [Fact]
    public async Task GivenProvisionedUserWithoutHousehold_WhenFetchingProfile_OnboardingIncomplete()
    {
        string externalId = Guid.NewGuid().ToString();
        string email = $"{Guid.NewGuid():N}@menlo.test";
        string displayName = $"User {Guid.NewGuid():N}";

        await using TestWebApplicationFactory factory = fixture.CreateFactory(externalId, email, displayName);
        User createdUser = await OnboardingApiTestData.ProvisionUserAsync(fixture.Services, externalId, email, displayName);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/auth/user", TestContext.Current.CancellationToken);
        UserProfile? profile = await response.Content.ReadFromJsonAsync<UserProfile>(TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        profile.ShouldNotBeNull();
        profile.Id.ShouldBe(createdUser.Id.Value.ToString());
        profile.Onboarding.ShouldNotBeNull();
        profile.Onboarding.IsComplete.ShouldBeFalse();
        profile.Onboarding.PendingTasks.ShouldContain("SelectHousehold");
    }

    [Fact]
    public async Task GivenProvisionedUserWithHousehold_WhenFetchingProfile_OnboardingComplete()
    {
        string externalId = Guid.NewGuid().ToString();
        string email = $"{Guid.NewGuid():N}@menlo.test";
        string displayName = $"User {Guid.NewGuid():N}";

        await using TestWebApplicationFactory factory = fixture.CreateFactory(externalId, email, displayName);
        User createdUser = await OnboardingApiTestData.ProvisionUserAsync(fixture.Services, externalId, email, displayName, completeOnboarding: true);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/auth/user", TestContext.Current.CancellationToken);
        UserProfile? profile = await response.Content.ReadFromJsonAsync<UserProfile>(TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        profile.ShouldNotBeNull();
        profile.Id.ShouldBe(createdUser.Id.Value.ToString());
        profile.Onboarding.ShouldNotBeNull();
        profile.Onboarding.IsComplete.ShouldBeTrue();
        profile.Onboarding.PendingTasks.ShouldBeEmpty();
    }
}
