using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Menlo.Api.Tests.Auth;

public sealed class CookieAuthenticationConfigurationTests
{
    [Fact]
    public void GivenTheAuthenticationCookieConfiguration_WhenResolvingCookieOptions()
    {
        using TestWebApplicationFactory factory = new();

        IOptionsMonitor<CookieAuthenticationOptions> optionsMonitor =
            factory.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>();
        CookieAuthenticationOptions options = optionsMonitor.Get(CookieAuthenticationDefaults.AuthenticationScheme);

        ItShouldUseStrictSameSiteCookies(options);
    }

    private static void ItShouldUseStrictSameSiteCookies(CookieAuthenticationOptions options)
    {
        options.Cookie.SameSite.ShouldBe(SameSiteMode.Strict);
    }
}
