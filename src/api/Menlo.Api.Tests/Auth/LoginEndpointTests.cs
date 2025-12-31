using System.Net;
using Menlo.Api.Tests.Fixtures;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Menlo.Api.Tests.Auth;

/// <summary>
/// Tests for LoginEndpoint.
/// </summary>
public sealed class LoginEndpointTests : TestFixture, IDisposable
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public LoginEndpointTests()
    {
        _factory = new TestWebApplicationFactory();
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task GivenNoReturnUrl_WhenInitiatingLogin()
    {
        HttpResponseMessage response = await _client.GetAsync("/auth/login", TestContext.Current.CancellationToken);

        ItShouldRedirect(response);
        ItShouldHaveLocationHeader(response);
    }

    [Fact]
    public async Task GivenValidRelativeReturnUrl_WhenInitiatingLogin()
    {
        HttpResponseMessage response = await _client.GetAsync("/auth/login?returnUrl=/dashboard", TestContext.Current.CancellationToken);

        ItShouldRedirect(response);
        ItShouldHaveLocationHeader(response);
    }

    [Fact]
    public async Task GivenEmptyReturnUrl_WhenInitiatingLogin()
    {
        HttpResponseMessage response = await _client.GetAsync("/auth/login?returnUrl=", TestContext.Current.CancellationToken);

        ItShouldRedirect(response);
        ItShouldHaveLocationHeader(response);
    }

    [Fact]
    public async Task GivenAbsoluteReturnUrl_WhenInitiatingLogin()
    {
        HttpResponseMessage response = await _client.GetAsync("/auth/login?returnUrl=https://evil.com", TestContext.Current.CancellationToken);

        ItShouldRedirect(response);
        ItShouldHaveLocationHeader(response);
    }

    [Fact]
    public async Task GivenProtocolRelativeUrl_WhenInitiatingLogin()
    {
        HttpResponseMessage response = await _client.GetAsync("/auth/login?returnUrl=//evil.com", TestContext.Current.CancellationToken);

        ItShouldRedirect(response);
        ItShouldHaveLocationHeader(response);
    }

    [Fact]
    public async Task GivenValidNestedPath_WhenInitiatingLogin()
    {
        HttpResponseMessage response = await _client.GetAsync("/auth/login?returnUrl=/budget/2024/january", TestContext.Current.CancellationToken);

        ItShouldRedirect(response);
        ItShouldHaveLocationHeader(response);
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
