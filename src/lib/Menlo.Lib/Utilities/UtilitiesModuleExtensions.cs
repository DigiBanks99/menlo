using Menlo.Common;
using Menlo.Utilities.Handlers.Electricity;
using Menlo.Utilities.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Menlo.Utilities;

public static class UtilitiesModuleExtensions
{
    public static IServiceCollection AddUtilitiesModule(this IServiceCollection services)
    {
        services.AddCosmosRepository(options =>
        {
            options.ContainerPerItemType = true;

            options.ContainerBuilder.Configure<ElectricityUsage>(containerOptions => containerOptions.WithContainer(nameof(ElectricityUsage)));
        });

        services
            .AddScoped<ICommandHandler<CaptureElectricityUsageRequest, string>, CaptureElectricityUsageHandler>()
            .AddScoped<IQueryHandler<ElectricityUsageQuery, IEnumerable<ElectricityUsageQueryResponse>>, ElectricityUsageQueryHandler>();

        return services;
    }
}
