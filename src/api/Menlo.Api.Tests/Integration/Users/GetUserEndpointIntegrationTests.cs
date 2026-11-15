using Menlo.Lib.Auth.Entities;
using System.Net;
using System.Text.Json;

namespace Menlo.Api.Tests.Integration.Users;

[ClassDataSource<UserApiFixture>(Shared = SharedType.PerTestSession)]
[NotInParallel("User API")]
public sealed class GetUserEndpointIntegrationTests(UserApiFixture fixture) : TestFixture
{
    [Test]
    public async Task GivenProvisionedUser_WhenFetchingProfile_ProfileHasNoOnboardingField()
    {
        string externalId = Guid.NewGuid().ToString();
        string email = $"{Guid.NewGuid():N}@menlo.test";
        string displayName = $"User {Guid.NewGuid():N}";

        await using TestWebApplicationFactory factory = fixture.CreateFactory(externalId, email, displayName);
        User createdUser = await UserApiTestData.ProvisionUserAsync(fixture.Services, externalId, email, displayName);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/auth/user", TestContext.Current!.Execution.CancellationToken);
        string body = await response.Content.ReadAsStringAsync(TestContext.Current!.Execution.CancellationToken);
        using JsonDocument profile = JsonDocument.Parse(body);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        profile.RootElement.GetProperty("id").GetString().ShouldBe(createdUser.Id.Value.ToString());
        profile.RootElement.TryGetProperty("onboarding", out _).ShouldBeFalse();
    }
}
