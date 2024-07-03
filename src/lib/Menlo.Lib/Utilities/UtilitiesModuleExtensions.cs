using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Menlo.Lib.Utilities;

public static class UtilitiesModuleExtensions
{
    public const string ModuleIdentifier = "Utilities";

    public static IServiceCollection AddUtilitiesModule(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<UtilitiesDbContext>(options => options.UseSqlServer(config.GetConnectionString(ModuleIdentifier)));

        return services;
    }
}
