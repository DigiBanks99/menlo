using Menlo.Api.Auth.Options;
using Menlo.Api.Auth.Policies;
using Menlo.Lib.Common.Abstractions;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;

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
        if (section.Get<MenloAuthOptions>() is null)
        {
            throw new InvalidOperationException("MenloAuthOptions configuration is missing.");
        }

        // AddMicrosoftIdentityWebApp reads ClientCertificates (or ClientSecret) from the AzureAd
        // config section. EnableTokenAcquisitionToCallDownstreamApi() is the critical step:
        // it registers MSAL and overrides OnAuthorizationCodeReceived so that MSAL — not the
        // native OpenIdConnectHandler — exchanges the authorization code at the token endpoint.
        // When ClientCertificates is configured (injected by CD from Key Vault), MSAL generates
        // the required client_assertion JWT instead of sending a plain-text client_secret,
        // satisfying Entra ID's AADSTS7000218 requirement.
        builder.Services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddMicrosoftIdentityWebApp(section)
            .EnableTokenAcquisitionToCallDownstreamApi()
            .AddInMemoryTokenCaches();

        // PostConfigure runs after AddMicrosoftIdentityWebApp's internal OidcConfigurator,
        // allowing us to layer Menlo-specific behaviour on top without losing MiW's
        // certificate-based token acquisition logic.
        builder.Services.PostConfigure<OpenIdConnectOptions>(
            OpenIdConnectDefaults.AuthenticationScheme,
            ConfigureOidcOptions);

        builder.Services.PostConfigure<CookieAuthenticationOptions>(
            CookieAuthenticationDefaults.AuthenticationScheme,
            ConfigureCookieOptions);

        builder.Services
            .AddAuthorizationBuilder()
            .AddMenloPolicies();

        return builder;
    }

    private static void ConfigureOidcOptions(OpenIdConnectOptions options)
    {
        options.SaveTokens = true;
        options.GetClaimsFromUserInfoEndpoint = true;

        if (!options.Scope.Contains("email"))
        {
            options.Scope.Add("email");
        }

        // Wrap the existing OnRedirectToIdentityProvider (set by AddMicrosoftIdentityWebApp's
        // OidcConfigurator) so both MiW logic and Menlo-specific behaviour run.
        Func<RedirectContext, Task> existingRedirect = options.Events.OnRedirectToIdentityProvider;
        options.Events.OnRedirectToIdentityProvider = async context =>
        {
            await existingRedirect(context);

            if (context.Handled)
            {
                return;
            }

            // Return 401 for XHR/fetch requests so Angular handles auth state gracefully
            // instead of receiving an unexpected redirect to Entra ID.
            if (IsXhrRequest(context.Request))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.HandleResponse();
                return;
            }

            // Safety net: rewrite redirect URI scheme when running behind a TLS-terminating
            // reverse proxy (e.g. Cloudflare Tunnel) in case ForwardedHeaders middleware
            // did not process X-Forwarded-Proto.
            if (context.ProtocolMessage.RedirectUri?.StartsWith("http://", StringComparison.OrdinalIgnoreCase) == true)
            {
                context.ProtocolMessage.RedirectUri =
                    string.Concat("https://", context.ProtocolMessage.RedirectUri.AsSpan(7));
            }
        };

        options.Events.OnRemoteFailure = context =>
        {
            ILogger logger = context.HttpContext.RequestServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger(typeof(AuthServiceCollectionExtensions).FullName!);

            logger.LogError(context.Failure, "OIDC authentication failed: {Error}", context.Failure?.Message);

            context.Response.Redirect("/sign-in?error=auth_failed");
            context.HandleResponse();
            return Task.CompletedTask;
        };
    }

    private static void ConfigureCookieOptions(CookieAuthenticationOptions options)
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
            // rather than redirect to OIDC login, which would overwrite the session cookie.
            if (IsXhrRequest(context.Request))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }

            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    }

    private static bool IsXhrRequest(HttpRequest request) =>
        request.Path.StartsWithSegments("/api")
        || request.Path.Equals("/auth/user", StringComparison.OrdinalIgnoreCase)
        || request.Headers.Accept.Any(a => a != null && a.Contains("application/json"))
        || request.Headers.ContainsKey("X-Requested-With");
}
