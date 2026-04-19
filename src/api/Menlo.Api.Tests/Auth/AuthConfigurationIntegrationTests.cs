using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Security.Claims;

namespace Menlo.Api.Tests.Auth;

public sealed class AuthConfigurationIntegrationTests : TestFixture
{
    [Fact]
    public async Task GetAsync_WithUnauthenticatedApiRequest()
    {
        using TestWebApplicationFactory factory = CreateRealAuthFactory();
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        HttpResponseMessage response = await client.GetAsync("/api/ai/health", TestContext.Current.CancellationToken);

        ItShouldBeUnauthorized(response);
        ItShouldNotRedirect(response);
    }

    [Fact]
    public async Task GetAsync_WithUnauthenticatedAuthUserRequest()
    {
        using TestWebApplicationFactory factory = CreateRealAuthFactory();
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        HttpResponseMessage response = await client.GetAsync("/auth/user", TestContext.Current.CancellationToken);

        ItShouldBeUnauthorized(response);
        ItShouldNotRedirect(response);
    }

    [Fact]
    public async Task GetAsync_WithNonXhrLoginRequest()
    {
        using TestWebApplicationFactory factory = CreateRealAuthFactory();
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        HttpResponseMessage response = await client.GetAsync("/auth/login", TestContext.Current.CancellationToken);

        ItShouldRedirect(response);
        ItShouldChallengeOidc(response);
    }

    [Fact]
    public async Task GetAsync_WithXhrProtectedRequest()
    {
        using TestWebApplicationFactory factory = CreateRealAuthFactory();
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");

        HttpResponseMessage response = await client.GetAsync("/auth/user", TestContext.Current.CancellationToken);

        ItShouldBeUnauthorized(response);
        ItShouldNotRedirect(response);
    }

    [Fact]
    public async Task GetAsync_WithCookieSignInSupport()
    {
        using TestWebApplicationFactory factory = CreateRealAuthFactory(app =>
        {
            app.Map("/test-support/sign-in", branch => branch.Run(async context =>
            {
                ClaimsPrincipal principal = new(new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, "test-user"),
                    new Claim(ClaimTypes.Name, "Test User"),
                    new Claim(ClaimTypes.Email, "test@example.com")
                ],
                    CookieAuthenticationDefaults.AuthenticationScheme));

                await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
                context.Response.StatusCode = StatusCodes.Status204NoContent;
            }));
        });
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        HttpResponseMessage response = await client.GetAsync("/test-support/sign-in", TestContext.Current.CancellationToken);

        ItShouldHaveNoContent(response);
        ItShouldSetStrictSessionCookie(response);
    }

    private static TestWebApplicationFactory CreateRealAuthFactory(Action<IApplicationBuilder>? configureApp = null) =>
        new()
        {
            UseTestAuthentication = false,
            ConfigureApp = configureApp
        };

    private static void ItShouldBeUnauthorized(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private static void ItShouldNotRedirect(HttpResponseMessage response)
    {
        response.Headers.Location.ShouldBeNull();
    }

    private static void ItShouldRedirect(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.Found);
        response.Headers.Location.ShouldNotBeNull();
    }

    private static void ItShouldChallengeOidc(HttpResponseMessage response)
    {
        response.Headers.Location.ShouldNotBeNull();
        response.Headers.Location.ToString().ShouldContain("https://login.test/authorize");
    }

    private static void ItShouldHaveNoContent(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    private static void ItShouldSetStrictSessionCookie(HttpResponseMessage response)
    {
        response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? cookieHeaders).ShouldBeTrue();
        cookieHeaders.ShouldNotBeNull();
        string sessionCookie = cookieHeaders.Single(header => header.StartsWith(".Menlo.Session="));
        sessionCookie.ShouldContain("SameSite=Strict");
    }
}
