# Menlo Persistence — Code Patterns

## MenloDbContext skeleton

```csharp
// Menlo.Application/Common/MenloDbContext.cs
public sealed class MenloDbContext : DbContext, IUserContext /*, IBudgetContext, etc. */
{
    public MenloDbContext(DbContextOptions<MenloDbContext> options) : base(options) { }

    // Shared / Auth
    public DbSet<User> Users => Set<User>();

    // (Add other bounded context DbSets as they are introduced)

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all IEntityTypeConfiguration<T> classes in this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MenloDbContext).Assembly);

        // Register global query filters for every ISoftDeletable entity
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var body = Expression.Equal(
                    Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted)),
                    Expression.Constant(false));
                modelBuilder.Entity(entityType.ClrType)
                    .HasQueryFilter(Expression.Lambda(body, parameter));
            }
        }
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Centrally map all strongly typed IDs (UserId, BudgetId, etc.) to uuid
        // Pattern: any readonly record struct wrapping a Guid with a Value property
        configurationBuilder
            .Properties<UserId>()
            .HaveConversion<UserIdValueConverter>();
        // Add other typed ID converters here as new entities are introduced
    }
}
```

## DI registration (`AddMenloApplication`)

```csharp
// Menlo.Application/Common/ServiceCollectionExtensions.cs
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMenloApplication(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<MenloDbContext>((sp, options) =>
        {
            options
                .UseNpgsql(connectionString)
                .UseSnakeCaseNamingConvention()
                .AddInterceptors(
                    sp.GetRequiredService<AuditingInterceptor>(),
                    sp.GetRequiredService<SoftDeleteInterceptor>());
        });

        // Interceptors
        services.AddScoped<AuditingInterceptor>();
        services.AddScoped<SoftDeleteInterceptor>();

        // Slice interface registrations
        services.AddScoped<IUserContext>(sp => sp.GetRequiredService<MenloDbContext>());
        // services.AddScoped<IBudgetContext>(sp => sp.GetRequiredService<MenloDbContext>());

        return services;
    }
}
```

## Slice interface (IUserContext)

```csharp
// Menlo.Application/Auth/IUserContext.cs
public interface IUserContext
{
    DbSet<User> Users { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

## Entity type configuration (User)

```csharp
// Menlo.Application/Auth/EntityConfigurations/UserEntityTypeConfiguration.cs
public sealed class UserEntityTypeConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users", "shared");

        // Primary key — typed ID, mapped to uuid by central value converter
        builder.HasKey(u => u.Id);

        // Required string constraints
        builder.Property(u => u.Email).HasMaxLength(256).IsRequired();
        builder.Property(u => u.DisplayName).HasMaxLength(100).IsRequired();

        // Unique index
        builder.HasIndex(u => u.Email).IsUnique();

        // Note: audit columns (created_by, created_at, modified_by, modified_at)
        // and soft-delete columns (is_deleted, deleted_at, deleted_by)
        // are created automatically from the entity's interface implementations.
        // No explicit configuration needed here.
    }
}
```

## AuditingInterceptor

```csharp
// Menlo.Application/Common/Interceptors/AuditingInterceptor.cs
public sealed class AuditingInterceptor : SaveChangesInterceptor
{
    private readonly IAuditStampFactory _factory;

    public AuditingInterceptor(IAuditStampFactory factory) => _factory = factory;

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null) return base.SavingChangesAsync(eventData, result, cancellationToken);

        foreach (var entry in eventData.Context.ChangeTracker.Entries<IAuditable>())
        {
            var operation = entry.State switch
            {
                EntityState.Added => AuditOperation.Create,
                EntityState.Modified => AuditOperation.Update,
                _ => (AuditOperation?)null
            };

            if (operation.HasValue)
                entry.Entity.Audit(_factory, operation.Value);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
```

## SoftDeleteInterceptor

```csharp
// Menlo.Application/Common/Interceptors/SoftDeleteInterceptor.cs
public sealed class SoftDeleteInterceptor : SaveChangesInterceptor
{
    private readonly ISoftDeleteStampFactory _factory;

    public SoftDeleteInterceptor(ISoftDeleteStampFactory factory) => _factory = factory;

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null) return base.SavingChangesAsync(eventData, result, cancellationToken);

        foreach (var entry in eventData.Context.ChangeTracker.Entries<ISoftDeletable>()
            .Where(e => e.State == EntityState.Deleted))
        {
            entry.State = EntityState.Modified;
            var stamp = _factory.CreateStamp();
            entry.Entity.MarkDeleted(stamp.ActorId, stamp.Timestamp);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
```

## Test fixture (TestContainers)

```csharp
// Menlo.Application.Tests/Fixtures/PersistenceFixture.cs
public sealed class PersistenceFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:17")
        .Build();

    public IServiceProvider Services { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var services = new ServiceCollection();
        services.AddMenloApplication(_container.GetConnectionString());
        services.AddScoped<IAuditStampFactory, TestAuditStampFactory>();
        services.AddScoped<ISoftDeleteStampFactory, TestSoftDeleteStampFactory>();

        Services = services.BuildServiceProvider();

        // Apply all migrations to a clean database
        using var scope = Services.CreateScope();
        await scope.ServiceProvider
            .GetRequiredService<MenloDbContext>()
            .Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();
}

// Test stub factories
public sealed class TestAuditStampFactory : IAuditStampFactory
{
    public static readonly UserId TestUserId = UserId.NewId();
    public AuditStamp CreateStamp() => new(TestUserId, DateTimeOffset.UtcNow);
}
```

## Example integration test

```csharp
[Collection("Persistence")]
public sealed class UserContextTests(PersistenceFixture fixture)
{
    [Fact]
    public async Task GivenNewUser_WhenSaved_ThenCanBeRetrieved()
    {
        using var scope = fixture.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<IUserContext>();

        var user = User.Create(new ExternalUserId("ext-123"), "test@menlo.app", "Test User").Value;
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();

        // Retrieve in a fresh scope (no change tracker cache)
        using var readScope = fixture.Services.CreateScope();
        var readCtx = readScope.ServiceProvider.GetRequiredService<IUserContext>();
        var retrieved = await readCtx.Users.FindAsync(user.Id);

        Assert.NotNull(retrieved);
        Assert.Equal("test@menlo.app", retrieved.Email);
        Assert.NotNull(retrieved.CreatedAt);   // AuditingInterceptor set this
        Assert.NotNull(retrieved.CreatedBy);
    }

    [Fact]
    public async Task GivenExistingUser_WhenDeleted_ThenExcludedFromStandardQuery()
    {
        using var scope = fixture.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<IUserContext>();

        var user = User.Create(new ExternalUserId("ext-soft-delete"), "soft@menlo.app", "Soft").Value;
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();

        ctx.Users.Remove(user);
        await ctx.SaveChangesAsync();

        // Standard query — should not see deleted record
        using var readScope = fixture.Services.CreateScope();
        var readCtx = readScope.ServiceProvider.GetRequiredService<IUserContext>();
        var found = await readCtx.Users.FindAsync(user.Id);
        Assert.Null(found);

        // IgnoreQueryFilters — should see it with IsDeleted = true
        var deleted = await readCtx.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == user.Id);
        Assert.NotNull(deleted);
        Assert.True(deleted.IsDeleted);
        Assert.NotNull(deleted.DeletedAt);
        Assert.NotNull(deleted.DeletedBy);
    }
}
```

## Migration commands

```sh
# Add a new migration
dotnet ef migrations add <MigrationName> \
  --project src/lib/Menlo.Application \
  --startup-project src/api/Menlo.Api

# Apply migrations manually (usually handled by MigrateAsync() on startup)
dotnet ef database update \
  --project src/lib/Menlo.Application \
  --startup-project src/api/Menlo.Api

# Remove last migration (if not yet applied to DB)
dotnet ef migrations remove \
  --project src/lib/Menlo.Application \
  --startup-project src/api/Menlo.Api
```
