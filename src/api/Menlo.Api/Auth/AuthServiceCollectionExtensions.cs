using Menlo.Api.Auth.Options;
using Menlo.Api.Auth.Policies;
using Menlo.Lib.Common.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
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

        builder.Services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddMicrosoftIdentityWebApp(section)
            .EnableTokenAcquisitionToCallDownstreamApi()
            .AddInMemoryTokenCaches();

        builder.Services.AddSingleton<IPostConfigureOptions<OpenIdConnectOptions>, MenloOpenIdConnectPostConfigure>();

        builder.Services.PostConfigure<CookieAuthenticationOptions>(
            CookieAuthenticationDefaults.AuthenticationScheme,
            ConfigureCookieOptions);

        builder.Services
            .AddAuthorizationBuilder()
            .AddMenloPolicies();

        builder.Services.AddScoped<IAuthorizationHandler, OnboardingCompleteHandler>();

        return builder;
    }

    internal static void ConfigureOidcOptions(OpenIdConnectOptions options, IServiceProvider serviceProvider)
    {
        options.SaveTokens = true;
        options.GetClaimsFromUserInfoEndpoint = true;
        options.Events ??= new OpenIdConnectEvents();

        if (!options.Scope.Contains("email"))
        {
            options.Scope.Add("email");
        }

        MenloOidcEvents menloEvents = new(serviceProvider);

        Func<TokenValidatedContext, Task> existingTokenValidated = options.Events.OnTokenValidated;
        options.Events.OnTokenValidated = async context =>
        {
            await existingTokenValidated(context);
            if (context.Result is not null)
            {
                return;
            }

            await menloEvents.OnTokenValidated(context);
        };

        Func<RedirectContext, Task> existingRedirect = options.Events.OnRedirectToIdentityProvider;
        options.Events.OnRedirectToIdentityProvider = async context =>
        {
            await existingRedirect(context);

            if (context.Handled)
            {
                return;
            }

            if (IsXhrRequest(context.Request))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.HandleResponse();
                return;
            }

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

internal sealed class MenloOpenIdConnectPostConfigure(IServiceProvider serviceProvider)
    : IPostConfigureOptions<OpenIdConnectOptions>
{
    public void PostConfigure(string? name, OpenIdConnectOptions options)
    {
        if (!string.Equals(name, OpenIdConnectDefaults.AuthenticationScheme, StringComparison.Ordinal))
        {
            return;
        }

        AuthServiceCollectionExtensions.ConfigureOidcOptions(options, serviceProvider);
    }
}
