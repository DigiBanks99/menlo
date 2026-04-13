using Menlo.Application.Common.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace Menlo.Application.Common;

public static class ServiceCollectionExtensions
{
    public static IHostApplicationBuilder AddMenloApplication(this IHostApplicationBuilder builder)
    {
        builder.Services
            .AddScoped<AuditingInterceptor>()
            .AddScoped<SoftDeleteInterceptor>()
            .AddDbContext<MenloDbContext>((sp, options) =>
            {
                IConfiguration config = sp.GetRequiredService<IConfiguration>();
                string connectionString = config.GetConnectionString("menlo")
                                          ?? throw new InvalidOperationException(
                                              "Connection string 'menlo' is not configured.");

                if (builder.Environment.IsDevelopment())
                {
                    NpgsqlConnectionStringBuilder csBuilder = new(connectionString) { SslMode = SslMode.Require };
                    connectionString = csBuilder.ConnectionString;
                }

                options
                    .UseNpgsql(connectionString)
                    .UseSnakeCaseNamingConvention()
                    .AddInterceptors(
                        sp.GetRequiredService<AuditingInterceptor>(),
                        sp.GetRequiredService<SoftDeleteInterceptor>());
            });

        return builder;
    }

    /// <summary>
    /// Applies any pending EF Core migrations. Call this during application startup
    /// before the host begins serving requests. Throws on failure so the process exits
    /// with a non-zero code and the container is marked unhealthy.
    /// </summary>
    public static async Task MigrateDatabaseAsync(
        this IHost host,
        CancellationToken cancellationToken = default)
    {
        await using AsyncServiceScope scope = host.Services.CreateAsyncScope();
        IConfiguration config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        if (config.GetValue<bool>("Menlo:SkipMigration"))
        {
            return;
        }

        MenloDbContext db = scope.ServiceProvider.GetRequiredService<MenloDbContext>();
        await db.Database.MigrateAsync(cancellationToken);
    }
}
