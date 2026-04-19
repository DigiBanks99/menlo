using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using System.Net;
using System.Net.Sockets;

namespace Menlo.Api.Tests;

public sealed class SpaDevelopmentProxyTests : TestFixture
{
    [Fact]
    public async Task GetAsync_WithSpaRouteInDevelopment_ProxiesToSpaBackend()
    {
        await using DevelopmentSpaBackend spaBackend = await DevelopmentSpaBackend.StartAsync();
        using TestWebApplicationFactory factory = CreateDevelopmentFactory(spaBackend.BaseAddress);
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        HttpResponseMessage response = await client.GetAsync("/workspaces/279?filter=open", TestContext.Current.CancellationToken);
        string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        ItShouldHaveSucceeded(response);
        ItShouldHaveBeenProxied(response, body, "/workspaces/279?filter=open");
    }

    [Fact]
    public async Task GetAsync_WithApiAuthOrHealthRouteInDevelopment_DoesNotProxyToSpaBackend()
    {
        await using DevelopmentSpaBackend spaBackend = await DevelopmentSpaBackend.StartAsync();
        using TestWebApplicationFactory factory = CreateDevelopmentFactory(spaBackend.BaseAddress);
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        HttpResponseMessage apiResponse = await client.GetAsync("/api/weatherforecast", TestContext.Current.CancellationToken);
        string apiBody = await apiResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        HttpResponseMessage authResponse = await client.GetAsync("/auth/user", TestContext.Current.CancellationToken);
        string authBody = await authResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        HttpResponseMessage healthResponse = await client.GetAsync("/health", TestContext.Current.CancellationToken);
        string healthBody = await healthResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        ItShouldHaveSucceeded(apiResponse);
        ItShouldHaveSucceeded(authResponse);
        ItShouldHaveSucceeded(healthResponse);
        ItShouldNotHaveBeenProxied(apiResponse, apiBody);
        ItShouldNotHaveBeenProxied(authResponse, authBody);
        ItShouldNotHaveBeenProxied(healthResponse, healthBody);
    }

    private static TestWebApplicationFactory CreateDevelopmentFactory(Uri spaBackendBaseAddress) =>
        new()
        {
            EnvironmentName = Environments.Development,
            ConfigurationOverrides = new Dictionary<string, string?>
            {
                ["ConnectionStrings:web-ui"] = spaBackendBaseAddress.ToString()
            }
        };

    private static void ItShouldHaveBeenProxied(HttpResponseMessage response, string body, string requestPathAndQuery)
    {
        response.Headers.TryGetValues("X-Spa-Backend", out IEnumerable<string>? values).ShouldBeTrue();
        values.ShouldContain("true");
        body.ShouldContain($"SPA BACKEND: {requestPathAndQuery}");
    }

    private static void ItShouldNotHaveBeenProxied(HttpResponseMessage response, string body)
    {
        response.Headers.Contains("X-Spa-Backend").ShouldBeFalse();
        body.ShouldNotContain("SPA BACKEND:");
    }

    private sealed class DevelopmentSpaBackend(WebApplication app, Uri baseAddress) : IAsyncDisposable
    {
        public Uri BaseAddress { get; } = baseAddress;

        public static async Task<DevelopmentSpaBackend> StartAsync()
        {
            int port = GetAvailablePort();
            WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                EnvironmentName = Environments.Development
            });

            builder.WebHost.UseUrls($"http://127.0.0.1:{port}");

            WebApplication app = builder.Build();
            app.Map("/{**path}", async context =>
            {
                string requestPathAndQuery = $"{context.Request.Path}{context.Request.QueryString}";
                context.Response.StatusCode = StatusCodes.Status200OK;
                context.Response.ContentType = "text/plain";
                context.Response.Headers["X-Spa-Backend"] = "true";
                await context.Response.WriteAsync($"SPA BACKEND: {requestPathAndQuery}", TestContext.Current.CancellationToken);
            });

            await app.StartAsync(TestContext.Current.CancellationToken);

            return new DevelopmentSpaBackend(app, new Uri($"http://127.0.0.1:{port}/"));
        }

        public async ValueTask DisposeAsync()
        {
            await app.StopAsync(TestContext.Current.CancellationToken);
            await app.DisposeAsync();
        }

        private static int GetAvailablePort()
        {
            using TcpListener listener = new(IPAddress.Loopback, 0);
            listener.Start();
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
    }
}

