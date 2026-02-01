using Menlo.Api.Persistence.Data;
using Menlo.Api.Persistence.Interceptors;
using Menlo.Lib.Common.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Menlo.Api.Persistence;

/// <summary>
/// Extension methods for registering persistence services.
/// </summary>
public static class PersistenceServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Menlo persistence layer services to the service collection.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IHostApplicationBuilder AddMenloPersistence(this IHostApplicationBuilder builder)
    {
        // Register HttpContextAccessor for audit stamp factory
        builder.Services.AddHttpContextAccessor();

        // Register TimeProvider (system default)
        builder.Services.AddSingleton(TimeProvider.System);

        // Register audit stamp factory
        builder.Services.AddScoped<IAuditStampFactory, AuditStampFactory>();

        // Register interceptors
        builder.Services.AddScoped<ISaveChangesInterceptor, AuditingInterceptor>();
        builder.Services.AddScoped<ISaveChangesInterceptor, SoftDeleteInterceptor>();

        // Use hybrid approach: AddDbContext for DI resolution + EnrichNpgsqlDbContext for Aspire features
        builder.Services.AddDbContext<MenloDbContext>((sp, options) =>
        {
            // Resolve interceptors from DI container
            options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());

            // Configure PostgreSQL connection
            string? connectionString = builder.Configuration.GetConnectionString("menlo");
            if (!string.IsNullOrEmpty(connectionString))
            {
                options.UseNpgsql(connectionString);
            }
        });

        // Enrich with Aspire features (health checks, telemetry, etc.)
        builder.EnrichNpgsqlDbContext<MenloDbContext>();

        return builder;
    }
}
