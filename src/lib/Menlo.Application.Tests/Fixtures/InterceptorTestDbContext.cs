using Menlo.Application.Common;
using Menlo.Lib.Common.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.DependencyInjection;

namespace Menlo.Application.Tests.Fixtures;

internal sealed class InterceptorTestDbContext(DbContextOptions<InterceptorTestDbContext> options) : DbContext(options)
{
    public DbSet<TestAuditableEntity> AuditableEntities => Set<TestAuditableEntity>();

    public DbSet<TestSoftDeletableEntity> SoftDeletableEntities => Set<TestSoftDeletableEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestAuditableEntity>(entity =>
        {
            entity.ToTable("test_auditable_entities");
            entity.HasKey(x => x.Id);
        });

        modelBuilder.Entity<TestSoftDeletableEntity>(entity =>
        {
            entity.ToTable("test_soft_deletable_entities");
            entity.HasKey(x => x.Id);
        });
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<UserId>()
            .HaveConversion<TestUserIdValueConverter>();
    }
}

internal sealed class TestUserIdValueConverter : ValueConverter<UserId, Guid>
{
    public TestUserIdValueConverter()
        : base(id => id.Value, value => new UserId(value))
    {
    }
}

internal static class InterceptorTestDbContextFactory
{
    private static readonly Type AuditingInterceptorType = typeof(MenloDbContext).Assembly
        .GetType("Menlo.Application.Common.Interceptors.AuditingInterceptor", throwOnError: true)!;

    private static readonly Type SoftDeleteInterceptorType = typeof(MenloDbContext).Assembly
        .GetType("Menlo.Application.Common.Interceptors.SoftDeleteInterceptor", throwOnError: true)!;

    public static InterceptorTestDbContext Create(IServiceProvider services, string connectionString)
    {
        DbContextOptions<InterceptorTestDbContext> options = new DbContextOptionsBuilder<InterceptorTestDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(GetProductionInterceptors(services))
            .Options;

        return new InterceptorTestDbContext(options);
    }

    // Resolve the production interceptors through DI so the tests exercise the same
    // save pipeline without widening interceptor visibility.
    private static IInterceptor[] GetProductionInterceptors(IServiceProvider services)
    {
        return
        [
            (IInterceptor)services.GetRequiredService(AuditingInterceptorType),
            (IInterceptor)services.GetRequiredService(SoftDeleteInterceptorType)
        ];
    }
}
