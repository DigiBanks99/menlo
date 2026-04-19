using Menlo.Api.Auth.Options;
using Menlo.Api.Auth.Policies;
using Menlo.Lib.Common.Abstractions;
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
    /// <param name="builder">The host application builder to extend.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IHostApplicationBuilder AddMenloAuthentication(this IHostApplicationBuilder builder)
    {
        builder.Services
            .AddOptions<MenloAuthOptions>()
            .BindConfiguration(MenloAuthOptions.SectionName)
            .ValidateOnStart();

        builder.Services.AddSingleton<IValidateOptions<MenloAuthOptions>, MenloAuthOptionsValidator>();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<CurrentUserPersistenceStampFactory>();
        builder.Services.AddScoped<IAuditStampFactory>(sp => sp.GetRequiredService<CurrentUserPersistenceStampFactory>());
        builder.Services.AddScoped<ISoftDeleteStampFactory>(
            sp => sp.GetRequiredService<CurrentUserPersistenceStampFactory>());

        IConfigurationSection section = builder.Configuration.GetSection(MenloAuthOptions.SectionName);
        MenloAuthOptions authOptions = section
            .Get<MenloAuthOptions>()
            ?? throw new InvalidOperationException("MenloAuthOptions configuration is missing.");

        // Configure authentication with dual schemes (Cookie + OIDC)
        builder.Services
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
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = true;
                options.Events.OnRedirectToLogin = context =>
                {
                    // Return 401 for XHR/fetch calls (API and auth endpoints accessed programmatically)
                    // rather than redirect to OIDC login, which would overwrite the session cookie
                    if (IsXhrRequest(context.Request))
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
                // Return 401 instead of redirecting to AAD for programmatic/XHR requests
                // so Angular can handle auth state gracefully without OIDC overwriting the session cookie.
                options.Events.OnRedirectToIdentityProvider = context =>
                {
                    if (IsXhrRequest(context.Request))
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.HandleResponse();
                    }

                    return Task.CompletedTask;
                };
            });

        builder.Services
            .AddAuthorizationBuilder()
            .AddMenloPolicies();

        return builder;
    }

    private static bool IsXhrRequest(HttpRequest request) =>
        request.Path.StartsWithSegments("/api")
        || request.Path.Equals("/auth/user", StringComparison.OrdinalIgnoreCase)
        || request.Headers.Accept.Any(a => a != null && a.Contains("application/json"))
        || request.Headers.ContainsKey("X-Requested-With");
}
