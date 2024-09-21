using Menlo.Auth;

namespace Menlo.Utilities;

internal static class UtilitiesModuleExtensions
{
    internal static WebApplicationBuilder AddUtilitiesModule(this WebApplicationBuilder builder, AuthorizationBuilder authorizationBuilder)
    {
        builder.Services.AddUtilitiesModule(builder.Configuration);

        authorizationBuilder.AddUtilitiesPolicy();

        return builder;
    }

    internal static AuthorizationBuilder AddUtilitiesPolicy(this AuthorizationBuilder builder)
    {
        return builder.AddPolicy(AuthConstants.PolicyNameUtilities, op => op.RequireRole("Utilities").Build());
    }

    internal static WebApplication UseUtilitiesModule(this WebApplication app)
    {
        bool isFeatureActive = app.IsFeatureActive();
        if (!isFeatureActive)
        {
            return app;
        }

        app.MapGroup("/api")
            .RequireAuthorizationWithBypass(app, AuthConstants.PolicyNameUtilities)
            .MapEndpoints();

        return app;
    }

    private static bool IsFeatureActive(this WebApplication app)
    {
        IFeatureManager featureManager = app.Services.GetRequiredService<IFeatureManager>();
        bool isEnabled = featureManager.IsModuleEnabledAsync(UtilitiesConstants.ModuleIdentifier).GetAwaiter().GetResult();
        return isEnabled;
    }
}
