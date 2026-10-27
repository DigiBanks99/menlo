using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

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
            c.Request.Path = "/api/ai/health";
        }, TestContext.Current.CancellationToken);

        ItShouldHaveStrictTransportSecurityHeader(ctx);
    }

    [Fact]
    public async Task GivenAuthenticatedUser_WhenMakingRequest_ThenResponseIncludesContentSecurityPolicyHeader()
    {
        HttpClient client = factory.CreateClient();
        HttpResponseMessage response = await client.GetAsync("/api/ai/health", TestContext.Current.CancellationToken);

        ItShouldHaveSucceeded(response);
        ItShouldHaveContentSecurityPolicyHeader(response);
    }

    [Fact]
    public async Task GivenAuthenticatedUser_WhenMakingRequest_ThenResponseIncludesXContentTypeOptionsHeader()
    {
        HttpClient client = factory.CreateClient();
        HttpResponseMessage response = await client.GetAsync("/api/ai/health", TestContext.Current.CancellationToken);

        ItShouldHaveSucceeded(response);
        ItShouldHaveXContentTypeOptionsHeader(response);
    }

    [Fact]
    public async Task GivenProduction_WhenMakingRequest_ThenCspAllowsDataImagesAndNoInlineScripts()
    {
        HttpClient client = factory.CreateClient();
        HttpResponseMessage response = await client.GetAsync("/api/ai/health", TestContext.Current.CancellationToken);

        string imgSrc = GetCspDirective(response, "img-src");
        string scriptSrc = GetCspDirective(response, "script-src");
        ItShouldAllowDataImages(imgSrc);
        ItShouldNotAllowUnsafeInline(scriptSrc);
    }

    [Fact]
    public async Task GivenDevelopment_WhenMakingRequest_ThenCspAllowsDataImagesAndInlineScripts()
    {
        using TestWebApplicationFactory devFactory = new()
        {
            EnvironmentName = Environments.Development,
            ConfigurationOverrides = new Dictionary<string, string?>
            {
                ["ConnectionStrings:web-ui"] = "http://localhost:4200"
            }
        };
        HttpClient client = devFactory.CreateClient();
        HttpResponseMessage response = await client.GetAsync("/api/ai/health", TestContext.Current.CancellationToken);

        string imgSrc = GetCspDirective(response, "img-src");
        string scriptSrc = GetCspDirective(response, "script-src");
        ItShouldAllowDataImages(imgSrc);
        scriptSrc.ShouldContain("'unsafe-inline'");
    }

    private static string GetCspDirective(HttpResponseMessage response, string directive)
    {
        string csp = response.Headers.GetValues("Content-Security-Policy").Single();
        return csp.Split(';', StringSplitOptions.TrimEntries)
            .Single(d => d.StartsWith(directive + " ", StringComparison.Ordinal));
    }

    private static void ItShouldAllowDataImages(string imgSrc)
    {
        imgSrc.ShouldContain("data:");
        imgSrc.ShouldContain("'self'");
    }

    private static void ItShouldNotAllowUnsafeInline(string scriptSrc)
    {
        scriptSrc.ShouldNotContain("'unsafe-inline'");
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
