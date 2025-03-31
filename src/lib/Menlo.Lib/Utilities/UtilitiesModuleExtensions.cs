using Azure.Core;
using Azure.Identity;
using Menlo.Common;
using Menlo.Utilities.Handlers.Electricity;
using Menlo.Utilities.Handlers.Water;
using Menlo.Utilities.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Menlo.Utilities;

public static class UtilitiesModuleExtensions
{
    public static IServiceCollection AddUtilitiesModule(
        this IServiceCollection services,
        IConfiguration configuration,
        bool isAzureEnvironment = false)
    {
        services.AddCosmosRepository(options =>
        {
            options.DatabaseId = "menlo";
            options.ContainerPerItemType = true;
            IConfiguration repoConfig = configuration.GetSection("RepositoryOptions");
            bool useTokenCredential = repoConfig.GetValue("UseTokenCredential", true);
            if (useTokenCredential)
            {
                TokenCredential credentialChain = isAzureEnvironment
                    ? new ChainedTokenCredential(new VisualStudioCredential(), new AzureCliCredential())
                    : new ChainedTokenCredential(new ManagedIdentityCredential());
                options.TokenCredential = credentialChain;
            }

            options.ContainerBuilder.Configure<ElectricityUsage>(containerOptions =>
                containerOptions.WithContainer(nameof(ElectricityUsage)));
            options.ContainerBuilder.Configure<ElectricityPurchase>(containerOptions =>
                containerOptions.WithContainer(nameof(ElectricityPurchase)));
        });

        return services
            .AddScoped<ICommandHandler<CaptureElectricityUsageRequest, string>, CaptureElectricityUsageHandler>()
            .AddScoped<IQueryHandler<ElectricityUsageQuery, IEnumerable<ElectricityUsageQueryResponse>>, ElectricityUsageQueryHandler>()
            .AddScoped<ICommandHandler<CaptureElectricityPurchaseRequest, string>, CaptureElectricityPurchaseHandler>()
            .AddScoped<ICommandHandler<CaptureWaterReadingCommand, string>, CaptureWaterReadingHandler>();
    }
}
