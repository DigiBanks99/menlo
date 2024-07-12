using Microsoft.FeatureManagement;

namespace Menlo.Utilities;

internal static class UtilitiesModuleExtensions
{
    internal static WebApplicationBuilder AddUtilitiesModule(this WebApplicationBuilder builder)
    {
        builder.Services.AddUtilitiesModule();

        return builder;
    }

    internal static WebApplication UseUtilitiesModule(this WebApplication app)
    {
        IFeatureManager featureManager = app.Services.GetRequiredService<IFeatureManager>();
        bool isEnabled = featureManager.IsModuleEnabledAsync(UtilitiesConstants.ModuleIdentifier).GetAwaiter().GetResult();
        if (isEnabled)
        {
            app.MapEndpoints();
        }

        return app;
    }
}
