using Menlo.Application.Auth;
using Menlo.Application.Common.Interceptors;
using Menlo.Lib.Common.Abstractions;
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
            .AddScoped<IAuditStampFactory, ReflectiveAuditStampFactory>()
            .AddScoped<ISoftDeleteStampFactory, ReflectiveAuditStampFactory>()
            .AddScoped<AuditingInterceptor>()
            .AddScoped<SoftDeleteInterceptor>()
            .AddDbContext<MenloDbContext>((sp, options) =>
            {
                IConfiguration config = sp.GetRequiredService<IConfiguration>();
                string connectionString = config.GetConnectionString("menlo")
                                          ?? throw new InvalidOperationException(
                                              "Connection string 'menlo' is not configured.");

                NpgsqlConnectionStringBuilder csBuilder = new(connectionString);

                if (!HasExplicitSslMode(connectionString))
                {
                    csBuilder.SslMode = ShouldDisableSsl(builder.Environment, csBuilder)
                        ? SslMode.Disable
                        : SslMode.Require;
                }

                connectionString = csBuilder.ConnectionString;

                options
                    .UseNpgsql(connectionString)
                    .UseSnakeCaseNamingConvention()
                    .AddInterceptors(
                        sp.GetRequiredService<AuditingInterceptor>(),
                        sp.GetRequiredService<SoftDeleteInterceptor>())
                    .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
            });

        builder.Services.AddScoped<IUserContext>(sp => sp.GetRequiredService<MenloDbContext>());
        builder.Services.AddScoped<IHouseholdContext>(sp => sp.GetRequiredService<MenloDbContext>());

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

    private static bool HasExplicitSslMode(string connectionString)
    {
        return connectionString.Contains("SSL Mode", StringComparison.OrdinalIgnoreCase)
               || connectionString.Contains("SslMode", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ShouldDisableSsl(IHostEnvironment environment, NpgsqlConnectionStringBuilder connectionStringBuilder)
    {
        if (environment.IsDevelopment())
        {
            return true;
        }

        string? host = connectionStringBuilder.Host;

        return string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
               || string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase)
               || string.Equals(host, "::1", StringComparison.OrdinalIgnoreCase);
    }
}


