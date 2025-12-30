using System.Net;
using System.Net.Http.Json;
using Menlo.Api.Tests.Fixtures;
using Menlo.Lib.Auth.Models;

namespace Menlo.Api.Tests.Auth;

public sealed class UserEndpointTests : TestFixture, IDisposable
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public UserEndpointTests()
    {
        _factory = new TestWebApplicationFactory
        {
            UserRoles = ["Menlo.User", "Menlo.Admin"]
        };
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GivenAnAuthenticatedUser_WhenRequestingUserProfile_AndUserHasRoles()
    {
        HttpResponseMessage response = await _client.GetAsync("/auth/user", TestContext.Current.CancellationToken);
        UserProfile? userProfile = await response.Content.ReadFromJsonAsync<UserProfile>(TestContext.Current.CancellationToken);

        ItShouldHaveSucceeded(response);
        ItShouldHaveTheUserProfile(userProfile);
        ItShouldIncludeUserRoles(userProfile);
    }

    [Fact]
    public async Task GivenAnUnauthenticatedUser_WhenRequestingUserProfile()
    {
        using TestWebApplicationFactory unauthenticatedFactory = new()
        {
            SimulateUnauthenticated = true
        };
        using HttpClient unauthenticatedClient = unauthenticatedFactory.CreateClient();

        HttpResponseMessage response = await unauthenticatedClient.GetAsync("/auth/user", TestContext.Current.CancellationToken);

        ItShouldHaveBeenUnauthorised(response);
    }

    private static void ItShouldHaveTheUserProfile(UserProfile? userProfile)
    {
        userProfile.ShouldNotBeNull();
        userProfile.Id.ShouldBe(TestAuthHandler.DefaultUserId);
        userProfile.Email.ShouldBe(TestAuthHandler.DefaultEmail);
        userProfile.DisplayName.ShouldBe(TestAuthHandler.DefaultName);
    }

    private static void ItShouldIncludeUserRoles(UserProfile? userProfile)
    {
        userProfile.ShouldNotBeNull();
        userProfile.Roles.ShouldContain("Menlo.User");
        userProfile.Roles.ShouldContain("Menlo.Admin");
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
