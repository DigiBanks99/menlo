using Menlo.FeatureManagement;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.FeatureManagement;

public static class FeatureManagementExtensions
{
    public static IServiceCollection RegisterFeatureManagement(this IServiceCollection services)
    {
        services
            .AddFeatureManagement()
            .AddFeatureFilter<ModuleFeatureFilter>();

        return services;
    }

    public static Task<bool> IsModuleEnabledAsync(this IFeatureManager featureManager, string moduleIdentifier)
    {
        return featureManager.IsEnabledAsync("Modules", moduleIdentifier);
    }
}
