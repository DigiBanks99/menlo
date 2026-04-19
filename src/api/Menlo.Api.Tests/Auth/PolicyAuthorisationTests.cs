using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Headers;

namespace Menlo.Api.Tests.Auth;

public sealed class PolicyAuthorisationTests : TestFixture, IDisposable
{
    private const string ProtectedEndpoint = "/api/ai/health";

    private readonly TestWebApplicationFactory _standardUserFactory;
    private readonly HttpClient _standardUserClient;

    public PolicyAuthorisationTests()
    {
        _standardUserFactory = new TestWebApplicationFactory
        {
            UserRoles = ["Menlo.User"]
        };
        _standardUserClient = _standardUserFactory.CreateClient();
    }

    [Fact]
    public async Task GivenAnAuthenticatedUser_WhenAccessingProtectedApiEndpoint()
    {
        HttpResponseMessage response = await _standardUserClient.GetAsync(ProtectedEndpoint, TestContext.Current.CancellationToken);

        ItShouldHaveSucceeded(response);
    }

    [Fact]
    public async Task GivenAnUnauthenticatedUser_WhenAccessingProtectedApiEndpoint()
    {
        using TestWebApplicationFactory unauthenticatedFactory = new()
        {
            SimulateUnauthenticated = true
        };
        using HttpClient unauthenticatedClient = unauthenticatedFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        HttpResponseMessage response = await unauthenticatedClient.GetAsync(ProtectedEndpoint, TestContext.Current.CancellationToken);

        ItShouldHaveBeenUnauthorised(response);
        ItShouldNotHaveRedirected(response);
    }

    [Fact]
    public async Task GivenAnUnauthenticatedXhrRequest_WhenAccessingProtectedApiEndpoint()
    {
        using TestWebApplicationFactory unauthenticatedFactory = new()
        {
            SimulateUnauthenticated = true
        };
        using HttpClient unauthenticatedClient = unauthenticatedFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        using HttpRequestMessage request = new(HttpMethod.Get, ProtectedEndpoint);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Add("X-Requested-With", "XMLHttpRequest");

        HttpResponseMessage response = await unauthenticatedClient.SendAsync(request, TestContext.Current.CancellationToken);

        ItShouldHaveBeenUnauthorised(response);
        ItShouldNotHaveRedirected(response);
    }

    [Fact]
    public async Task GivenAnAdminUser_WhenAccessingProtectedApiEndpoint()
    {
        using TestWebApplicationFactory adminFactory = new()
        {
            UserRoles = ["Menlo.Admin"]
        };
        using HttpClient adminClient = adminFactory.CreateClient();

        HttpResponseMessage response = await adminClient.GetAsync(ProtectedEndpoint, TestContext.Current.CancellationToken);

        ItShouldHaveSucceeded(response);
    }

    private static void ItShouldNotHaveRedirected(HttpResponseMessage response)
    {
        response.Headers.Location.ShouldBeNull();
    }

    public void Dispose()
    {
        _standardUserClient.Dispose();
        _standardUserFactory.Dispose();
    }
}
