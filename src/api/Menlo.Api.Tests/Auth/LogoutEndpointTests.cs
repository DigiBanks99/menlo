using System.Net;
using Menlo.Api.Tests.Fixtures;

namespace Menlo.Api.Tests.Auth;

/// <summary>
/// Tests for LogoutEndpoint.
/// </summary>
public sealed class LogoutEndpointTests : TestFixture, IDisposable
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public LogoutEndpointTests()
    {
        _factory = new TestWebApplicationFactory();
        _client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task GivenAuthenticatedUser_WhenLoggingOut()
    {
        HttpResponseMessage response = await _client.PostAsync("/auth/logout", null, TestContext.Current.CancellationToken);

        ItShouldRedirect(response);
        ItShouldHaveLocationHeader(response);
    }

    [Fact]
    public async Task GivenAuthenticatedUser_WhenLoggingOutWithReturnUrl()
    {
        HttpResponseMessage response = await _client.PostAsync("/auth/logout?returnUrl=/home", null, TestContext.Current.CancellationToken);

        ItShouldRedirect(response);
        ItShouldHaveLocationHeader(response);
    }

    [Fact]
    public async Task GivenAuthenticatedUser_WhenLoggingOutWithEmptyReturnUrl()
    {
        HttpResponseMessage response = await _client.PostAsync("/auth/logout?returnUrl=", null, TestContext.Current.CancellationToken);

        ItShouldRedirect(response);
        ItShouldHaveLocationHeader(response);
    }

    [Fact]
    public async Task GivenUnauthenticatedUser_WhenAttemptingLogout()
    {
        using TestWebApplicationFactory unauthenticatedFactory = new()
        {
            SimulateUnauthenticated = true
        };
        using HttpClient unauthenticatedClient = unauthenticatedFactory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        HttpResponseMessage response = await unauthenticatedClient.PostAsync("/auth/logout", null, TestContext.Current.CancellationToken);

        ItShouldHaveBeenUnauthorised(response);
    }

    // Assertion Helpers
    private static void ItShouldRedirect(HttpResponseMessage response)
    {
        (response.StatusCode == HttpStatusCode.Found ||
         response.StatusCode == HttpStatusCode.Redirect ||
         response.StatusCode == HttpStatusCode.SeeOther ||
         response.StatusCode == HttpStatusCode.TemporaryRedirect).ShouldBeTrue();
    }

    private static void ItShouldHaveLocationHeader(HttpResponseMessage response)
    {
        response.Headers.Location.ShouldNotBeNull();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
