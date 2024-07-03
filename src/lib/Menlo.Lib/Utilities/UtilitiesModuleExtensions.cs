using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Menlo.Utilities;

public static class UtilitiesModuleExtensions
{
    public static IServiceCollection AddUtilitiesModule(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<UtilitiesDbContext>(options => options.UseSqlServer(config.GetConnectionString(UtilitiesConstants.ModuleIdentifier)));

        return services;
    }
}
