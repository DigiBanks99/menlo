using Microsoft.AspNetCore.Http;
using System.Net;

namespace Menlo.Api.Tests;

public sealed class ForwardedHeadersTests : TestFixture
{
    private static readonly IPAddress UntrustedClient = IPAddress.Parse("203.0.113.5");
    private static readonly IPAddress SpoofedClient = IPAddress.Parse("198.51.100.7");

    [Fact]
    public async Task GivenUntrustedSource_WhenSendingForwardedHeaders_ThenHeadersAreIgnored()
    {
        using TestWebApplicationFactory factory = new();

        HttpContext ctx = await SendAsync(factory, UntrustedClient);

        ItShouldIgnoreForwardedHeaders(ctx, UntrustedClient);
    }

    [Fact]
    public async Task GivenLoopbackSource_WhenSendingForwardedHeaders_ThenHeadersAreApplied()
    {
        using TestWebApplicationFactory factory = new();

        HttpContext ctx = await SendAsync(factory, IPAddress.Loopback);

        ItShouldApplyForwardedHeaders(ctx);
    }

    [Fact]
    public async Task GivenConfiguredKnownNetwork_WhenSourceIsInsideIt_ThenHeadersAreApplied()
    {
        using TestWebApplicationFactory factory = WithKnownNetworks("172.20.0.0/16");

        HttpContext ctx = await SendAsync(factory, IPAddress.Parse("172.20.0.1"));

        ItShouldApplyForwardedHeaders(ctx);
    }

    [Fact]
    public async Task GivenConfiguredKnownNetwork_WhenSourceIsOutsideIt_ThenHeadersAreIgnored()
    {
        using TestWebApplicationFactory factory = WithKnownNetworks("172.20.0.0/16");

        HttpContext ctx = await SendAsync(factory, UntrustedClient);

        ItShouldIgnoreForwardedHeaders(ctx, UntrustedClient);
    }

    [Fact]
    public async Task GivenConfiguredKnownNetwork_WhenSourceIsLoopback_ThenHeadersAreStillApplied()
    {
        using TestWebApplicationFactory factory = WithKnownNetworks("172.20.0.0/16");

        HttpContext ctx = await SendAsync(factory, IPAddress.Loopback);

        ItShouldApplyForwardedHeaders(ctx);
    }

    [Fact]
    public async Task GivenConfiguredKnownNetwork_WhenSourceIsIpv4MappedInsideIt_ThenHeadersAreApplied()
    {
        using TestWebApplicationFactory factory = WithKnownNetworks("172.20.0.0/16");

        HttpContext ctx = await SendAsync(factory, IPAddress.Parse("172.20.0.1").MapToIPv6());

        ItShouldApplyForwardedHeaders(ctx);
    }

    [Fact]
    public async Task GivenBlankKnownNetwork_WhenSourceIsUntrusted_ThenHeadersAreIgnored()
    {
        using TestWebApplicationFactory factory = WithKnownNetworks("");

        HttpContext ctx = await SendAsync(factory, UntrustedClient);

        ItShouldIgnoreForwardedHeaders(ctx, UntrustedClient);
    }

    [Fact]
    public async Task GivenConfiguredKnownProxy_WhenSourceIsThatProxy_ThenHeadersAreApplied()
    {
        using TestWebApplicationFactory factory = new()
        {
            ConfigurationOverrides = new Dictionary<string, string?>
            {
                ["ForwardedHeaders:KnownProxies:0"] = UntrustedClient.ToString()
            }
        };

        HttpContext ctx = await SendAsync(factory, UntrustedClient);

        ItShouldApplyForwardedHeaders(ctx);
    }

    [Fact]
    public void GivenInvalidKnownNetwork_WhenStartingHost_ThenStartupFails()
    {
        using TestWebApplicationFactory factory = WithKnownNetworks("not-a-cidr");

        Should.Throw<InvalidOperationException>(() => factory.CreateClient())
            .Message.ShouldContain("ForwardedHeaders:KnownNetworks");
    }

    private static TestWebApplicationFactory WithKnownNetworks(string network) => new()
    {
        ConfigurationOverrides = new Dictionary<string, string?>
        {
            ["ForwardedHeaders:KnownNetworks:0"] = network
        }
    };

    private static Task<HttpContext> SendAsync(TestWebApplicationFactory factory, IPAddress remoteIp) =>
        factory.Server.SendAsync(c =>
        {
            c.Request.Method = "GET";
            c.Request.Scheme = "http";
            c.Request.Host = new HostString("example.com");
            c.Request.Path = "/api/ai/health";
            c.Request.Headers["X-Forwarded-Proto"] = "https";
            c.Request.Headers["X-Forwarded-For"] = SpoofedClient.ToString();
            c.Connection.RemoteIpAddress = remoteIp;
        }, TestContext.Current.CancellationToken);

    private static void ItShouldIgnoreForwardedHeaders(HttpContext ctx, IPAddress expectedRemoteIp)
    {
        ctx.Request.Scheme.ShouldBe("http");
        ctx.Connection.RemoteIpAddress.ShouldBe(expectedRemoteIp);
    }

    private static void ItShouldApplyForwardedHeaders(HttpContext ctx)
    {
        ctx.Request.Scheme.ShouldBe("https");
        ctx.Connection.RemoteIpAddress.ShouldBe(SpoofedClient);
    }
}
