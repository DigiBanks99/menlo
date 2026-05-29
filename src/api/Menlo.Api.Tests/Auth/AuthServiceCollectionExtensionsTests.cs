using Menlo.Api.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Net;

namespace Menlo.Api.Tests.Auth;

/// <summary>
/// Tests for AuthServiceCollectionExtensions covering auth configuration,
/// XHR detection, cookie events, and OIDC redirect URI rewrite.
/// </summary>
public sealed class AuthServiceCollectionExtensionsTests : TestFixture
{
    [Fact]
    public async Task OnRedirectToIdentityProvider_WithHttpSchemeRequest()
    {
        using TestWebApplicationFactory factory = CreateRealAuthFactory();
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Send request with http:// scheme to trigger redirect URI rewrite
        HttpResponseMessage response = await client.GetAsync(
            "http://localhost/auth/login", TestContext.Current.CancellationToken);

        ItShouldRedirectToOidc(response);
        ItShouldHaveHttpsRedirectUri(response);
    }

    [Fact]
    public async Task OnRedirectToIdentityProvider_WithHttpsSchemeRequest()
    {
        using TestWebApplicationFactory factory = CreateRealAuthFactory();
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        HttpResponseMessage response = await client.GetAsync(
            "https://localhost/auth/login", TestContext.Current.CancellationToken);

        ItShouldRedirectToOidc(response);
        ItShouldHaveHttpsRedirectUri(response);
    }

    [Fact]
    public async Task OnRedirectToIdentityProvider_WithAcceptJsonHeader()
    {
        using TestWebApplicationFactory factory = CreateRealAuthFactory();
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Accept.Add(
            new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

        // /auth/login triggers OIDC challenge, Accept: application/json triggers XHR detection
        HttpResponseMessage response = await client.GetAsync(
            "/auth/login", TestContext.Current.CancellationToken);

        ItShouldBeUnauthorized(response);
        ItShouldNotRedirect(response);
    }

    [Fact]
    public async Task OnRedirectToLogin_WithXhrRequest()
    {
        using TestWebApplicationFactory factory = CreateRealAuthFactory(
            app => app.Map("/test/cookie-challenge", branch => branch.Run(
                context => context.ChallengeAsync(CookieAuthenticationDefaults.AuthenticationScheme))));
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");

        HttpResponseMessage response = await client.GetAsync(
            "/test/cookie-challenge", TestContext.Current.CancellationToken);

        ItShouldBeUnauthorized(response);
        ItShouldNotRedirect(response);
    }

    [Fact]
    public async Task OnRedirectToLogin_WithNonXhrBrowserRequest()
    {
        using TestWebApplicationFactory factory = CreateRealAuthFactory(
            app => app.Map("/test/cookie-challenge", branch => branch.Run(
                context => context.ChallengeAsync(CookieAuthenticationDefaults.AuthenticationScheme))));
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Accept.Add(
            new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/html"));

        HttpResponseMessage response = await client.GetAsync(
            "/test/cookie-challenge", TestContext.Current.CancellationToken);

        ItShouldRedirect(response);
    }

    [Fact]
    public void AddMenloAuthentication_WithMissingConfiguration()
    {
        // Build a minimal host with no AzureAd configuration to trigger the guard clause
        HostApplicationBuilder builder = new(new HostApplicationBuilderSettings
        {
            DisableDefaults = true
        });

        Action act = () => builder.AddMenloAuthentication();

        act.ShouldThrow<InvalidOperationException>()
            .Message.ShouldContain("MenloAuthOptions");
    }

    [Fact]
    public void CookieOptions_WithExpectedSecurityConfiguration()
    {
        using TestWebApplicationFactory factory = new();

        IOptionsMonitor<CookieAuthenticationOptions> optionsMonitor =
            factory.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>();
        CookieAuthenticationOptions options =
            optionsMonitor.Get(CookieAuthenticationDefaults.AuthenticationScheme);

        ItShouldUseHttpOnlyCookies(options);
        ItShouldRequireSecureCookies(options);
        ItShouldHaveEightHourExpiry(options);
        ItShouldUseSlidingExpiration(options);
        ItShouldUseCorrectCookieName(options);
    }

    [Fact]
    public void OpenIdConnectOptions_WithExpectedScopes()
    {
        using TestWebApplicationFactory factory = new();

        IOptionsMonitor<OpenIdConnectOptions> optionsMonitor =
            factory.Services.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>();
        OpenIdConnectOptions options =
            optionsMonitor.Get(OpenIdConnectDefaults.AuthenticationScheme);

        ItShouldRequestOpenIdScope(options);
        ItShouldRequestProfileScope(options);
        ItShouldRequestEmailScope(options);
        ItShouldUseAuthorizationCodeFlow(options);
        ItShouldSaveTokens(options);
    }

    // Factory helpers

    private static TestWebApplicationFactory CreateRealAuthFactory(Action<IApplicationBuilder>? configureApp = null) =>
        new() { UseTestAuthentication = false, ConfigureApp = configureApp };

    // Assertion helpers

    private static void ItShouldRedirectToOidc(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.Found);
        response.Headers.Location.ShouldNotBeNull();
        response.Headers.Location.ToString().ShouldContain("login.test/authorize");
    }

    private static void ItShouldHaveHttpsRedirectUri(HttpResponseMessage response)
    {
        string location = response.Headers.Location!.ToString();
        string redirectUriParam = Uri.UnescapeDataString(
            location.Split("redirect_uri=")[1].Split('&')[0]);
        redirectUriParam.ShouldStartWith("https://", Case.Insensitive);
    }

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
        ((int)response.StatusCode >= 300 && (int)response.StatusCode < 400).ShouldBeTrue(
            $"Expected redirect status but got {response.StatusCode}");
    }

    private static void ItShouldUseHttpOnlyCookies(CookieAuthenticationOptions options)
    {
        options.Cookie.HttpOnly.ShouldBeTrue();
    }

    private static void ItShouldRequireSecureCookies(CookieAuthenticationOptions options)
    {
        options.Cookie.SecurePolicy.ShouldBe(CookieSecurePolicy.Always);
    }

    private static void ItShouldHaveEightHourExpiry(CookieAuthenticationOptions options)
    {
        options.ExpireTimeSpan.ShouldBe(TimeSpan.FromHours(8));
    }

    private static void ItShouldUseSlidingExpiration(CookieAuthenticationOptions options)
    {
        options.SlidingExpiration.ShouldBeTrue();
    }

    private static void ItShouldUseCorrectCookieName(CookieAuthenticationOptions options)
    {
        options.Cookie.Name.ShouldBe(".Menlo.Session");
    }

    private static void ItShouldRequestOpenIdScope(OpenIdConnectOptions options)
    {
        options.Scope.ShouldContain("openid");
    }

    private static void ItShouldRequestProfileScope(OpenIdConnectOptions options)
    {
        options.Scope.ShouldContain("profile");
    }

    private static void ItShouldRequestEmailScope(OpenIdConnectOptions options)
    {
        options.Scope.ShouldContain("email");
    }

    private static void ItShouldUseAuthorizationCodeFlow(OpenIdConnectOptions options)
    {
        options.ResponseType.ShouldBe("code");
    }

    private static void ItShouldSaveTokens(OpenIdConnectOptions options)
    {
        options.SaveTokens.ShouldBeTrue();
    }
}
