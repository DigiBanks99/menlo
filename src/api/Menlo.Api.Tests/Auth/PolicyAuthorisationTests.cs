using System.Net;

namespace Menlo.Api.Tests.Auth;

public sealed class PolicyAuthorisationTests : TestFixture, IDisposable
{
    private const string ProtectedEndpoint = "/api/weatherforecast";

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
        using HttpClient unauthenticatedClient = unauthenticatedFactory.CreateClient();

        HttpResponseMessage response = await unauthenticatedClient.GetAsync(ProtectedEndpoint, TestContext.Current.CancellationToken);

        ItShouldHaveBeenUnauthorised(response);
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

    public void Dispose()
    {
        _standardUserClient.Dispose();
        _standardUserFactory.Dispose();
    }
}
