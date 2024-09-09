namespace Menlo.Auth;

public static class AuthHostExtensions
{
    public static AuthorizationBuilder AddAuth(this WebApplicationBuilder builder)
    {
        MicrosoftIdentityOptions? azureAdOptions = builder.Configuration
            .GetSection("AzureAd")
            .Get<MicrosoftIdentityOptions>();
        if (azureAdOptions is null)
        {
            throw new InvalidOperationException("AzureAd section is missing from configuration");
        }

        builder.Services
            .AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApp(options =>
            {
                options.ClientId = azureAdOptions.ClientId;
                options.ClientSecret = azureAdOptions.ClientSecret;
                options.Domain = azureAdOptions.Domain;
                options.Instance = azureAdOptions.Instance;
                options.TenantId = azureAdOptions.TenantId;
                options.CallbackPath = azureAdOptions.CallbackPath;
                options.SignedOutCallbackPath = azureAdOptions.SignedOutCallbackPath;

                options.Events.OnRedirectToIdentityProvider = ctx =>
                {
                    ctx.ProtocolMessage.RedirectUri = ctx.ProtocolMessage.RedirectUri.Replace("http://", "https://", StringComparison.OrdinalIgnoreCase);
                    return Task.CompletedTask;
                };
            });

        return builder.Services
            .AddAuthorizationBuilder()
            .AddPolicy(AuthConstants.PolicyNameAuthenticatedUsersOnly, op => op.RequireAuthenticatedUser().Build());
    }
}
