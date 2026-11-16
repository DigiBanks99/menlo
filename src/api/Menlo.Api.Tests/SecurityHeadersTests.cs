using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace Menlo.Api.Tests;

[ClassDataSource<TestWebApplicationFactory>(Shared = SharedType.PerClass)]
// The shared factory starts its server lazily without a lock, so this class's tests run one at a time.
[NotInParallel(nameof(SecurityHeadersTests))]
public sealed class SecurityHeadersTests(TestWebApplicationFactory factory) : TestFixture
{
    [Test]
    public async Task GivenAuthenticatedUser_WhenMakingRequest_ThenResponseIncludesStrictTransportSecurityHeader()
    {
        // HSTS is excluded for localhost by the library; use a real hostname + https scheme
        HttpContext ctx = await factory.Server.SendAsync(c =>
        {
            c.Request.Method = "GET";
            c.Request.Scheme = "https";
            c.Request.Host = new HostString("example.com");
            c.Request.Path = "/api/ai/health";
        }, TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveStrictTransportSecurityHeader(ctx);
    }

    [Test]
    public async Task GivenAuthenticatedUser_WhenMakingRequest_ThenResponseIncludesContentSecurityPolicyHeader()
    {
        HttpClient client = factory.CreateClient();
        HttpResponseMessage response = await client.GetAsync("/api/ai/health", TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveSucceeded(response);
        ItShouldHaveContentSecurityPolicyHeader(response);
    }

    [Test]
    public async Task GivenAuthenticatedUser_WhenMakingRequest_ThenResponseIncludesXContentTypeOptionsHeader()
    {
        HttpClient client = factory.CreateClient();
        HttpResponseMessage response = await client.GetAsync("/api/ai/health", TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveSucceeded(response);
        ItShouldHaveXContentTypeOptionsHeader(response);
    }

    [Test]
    public async Task GivenProduction_WhenMakingRequest_ThenCspAllowsDataImagesAndNoInlineScripts()
    {
        HttpClient client = factory.CreateClient();
        HttpResponseMessage response = await client.GetAsync("/api/ai/health", TestContext.Current!.Execution.CancellationToken);

        string imgSrc = GetCspDirective(response, "img-src");
        string scriptSrc = GetCspDirective(response, "script-src");
        ItShouldAllowDataImages(imgSrc);
        ItShouldNotAllowUnsafeInline(scriptSrc);
    }

    [Test]
    public async Task GivenDevelopment_WhenRequestingAPage_ThenCspIsAsStrictAsProduction()
    {
        using TestWebApplicationFactory devFactory = new() { EnvironmentName = Environments.Development };
        HttpClient client = devFactory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/design", TestContext.Current!.Execution.CancellationToken);

        ItShouldNotAllowUnsafeInline(GetCspDirective(response, "script-src"));
        ItShouldNotAllowUnsafeInline(GetCspDirective(response, "style-src"));
    }

    [Test]
    public async Task GivenDevelopment_WhenRequestingTheApiReference_ThenCspAllowsItsInlineScriptsAndStyles()
    {
        using TestWebApplicationFactory devFactory = new() { EnvironmentName = Environments.Development };
        HttpClient client = devFactory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/scalar/v1", TestContext.Current!.Execution.CancellationToken);

        GetCspDirective(response, "script-src").ShouldContain("unsafe-inline");
        GetCspDirective(response, "style-src").ShouldContain("unsafe-inline");
    }

    [Test]
    public async Task GivenProduction_WhenMakingRequest_ThenCspAllowsNoInlineStylesAndSignOutRedirect()
    {
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/ai/health", TestContext.Current!.Execution.CancellationToken);

        ItShouldNotAllowUnsafeInline(GetCspDirective(response, "style-src"));
        GetCspDirective(response, "form-action").ShouldContain("https://login.microsoftonline.com");
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
        scriptSrc.ShouldContain("'self'");
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
