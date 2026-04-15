using Microsoft.AspNetCore.Http;

namespace Menlo.Api.Tests;

public sealed class SecurityHeadersTests(TestWebApplicationFactory factory) : TestFixture, IClassFixture<TestWebApplicationFactory>
{
    [Fact]
    public async Task GivenAuthenticatedUser_WhenMakingRequest_ThenResponseIncludesStrictTransportSecurityHeader()
    {
        // HSTS is excluded for localhost by the library; use a real hostname + https scheme
        HttpContext ctx = await factory.Server.SendAsync(c =>
        {
            c.Request.Method = "GET";
            c.Request.Scheme = "https";
            c.Request.Host = new HostString("example.com");
            c.Request.Path = "/api/weatherforecast";
        }, TestContext.Current.CancellationToken);

        ItShouldHaveStrictTransportSecurityHeader(ctx);
    }

    [Fact]
    public async Task GivenAuthenticatedUser_WhenMakingRequest_ThenResponseIncludesContentSecurityPolicyHeader()
    {
        HttpClient client = factory.CreateClient();
        HttpResponseMessage response = await client.GetAsync("/api/weatherforecast", TestContext.Current.CancellationToken);

        ItShouldHaveSucceeded(response);
        ItShouldHaveContentSecurityPolicyHeader(response);
    }

    [Fact]
    public async Task GivenAuthenticatedUser_WhenMakingRequest_ThenResponseIncludesXContentTypeOptionsHeader()
    {
        HttpClient client = factory.CreateClient();
        HttpResponseMessage response = await client.GetAsync("/api/weatherforecast", TestContext.Current.CancellationToken);

        ItShouldHaveSucceeded(response);
        ItShouldHaveXContentTypeOptionsHeader(response);
    }

    private static void ItShouldHaveStrictTransportSecurityHeader(HttpContext ctx)
    {
        ctx.Response.Headers.ContainsKey("Strict-Transport-Security").ShouldBeTrue(
            "Response should include Strict-Transport-Security header");
    }

    private static void ItShouldHaveContentSecurityPolicyHeader(HttpResponseMessage response)
    {
        response.Headers.Contains("Content-Security-Policy").ShouldBeTrue(
            "Response should include Content-Security-Policy header");
    }

    private static void ItShouldHaveXContentTypeOptionsHeader(HttpResponseMessage response)
    {
        response.Headers.Contains("X-Content-Type-Options").ShouldBeTrue(
            "Response should include X-Content-Type-Options header");
    }
}
