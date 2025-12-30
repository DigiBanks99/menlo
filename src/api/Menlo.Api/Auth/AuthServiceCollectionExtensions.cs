using Menlo.Api.Auth.Options;
using Menlo.Api.Auth.Policies;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;

namespace Menlo.Api.Auth;

/// <summary>
/// Extension methods for configuring authentication services.
/// </summary>
public static class AuthServiceCollectionExtensions
{
    /// <summary>
    /// Adds Menlo authentication services with Entra ID integration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMenloAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<MenloAuthOptions>()
            .BindConfiguration(MenloAuthOptions.SectionName)
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<MenloAuthOptions>, MenloAuthOptionsValidator>();

        IConfigurationSection section = configuration.GetSection(MenloAuthOptions.SectionName);
        MenloAuthOptions authOptions = section
            .Get<MenloAuthOptions>()
            ?? throw new InvalidOperationException("MenloAuthOptions configuration is missing.");

        // Configure authentication with dual schemes (Cookie + OIDC)
        services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.Cookie.Name = ".Menlo.Session";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.Domain = authOptions.CookieDomain;
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = true;
                options.Events.OnRedirectToLogin = context =>
                {
                    // Return 401 for API calls instead of redirect
                    if (context.Request.Path.StartsWithSegments("/api"))
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return Task.CompletedTask;
                    }

                    context.Response.Redirect(context.RedirectUri);
                    return Task.CompletedTask;
                };
            })
            .AddOpenIdConnect(options =>
            {
                options.Authority = $"{authOptions.Instance}{authOptions.TenantId}/v2.0";
                options.ClientId = authOptions.ClientId;
                options.ClientSecret = authOptions.ClientSecret;
                options.ResponseType = "code";
                options.SaveTokens = true;
                options.GetClaimsFromUserInfoEndpoint = true;
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("email");
                options.CallbackPath = authOptions.CallbackPath;
                options.SignedOutCallbackPath = authOptions.SignedOutCallbackPath;
                options.TokenValidationParameters.RoleClaimType = "roles";
            });

        services
            .AddAuthorizationBuilder()
            .AddMenloPolicies();

        return services;
    }
}
