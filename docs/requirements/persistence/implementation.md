# Persistence Implementation Guide

This guide reflects the current Menlo persistence architecture based on `Menlo.Application` as the single EF Core + PostgreSQL integration point.

## Current architecture

- `src/lib/Menlo.Application/` owns EF Core, PostgreSQL provider configuration, migrations, interceptors, and slice interfaces.
- `src/api/Menlo.Api/` references `Menlo.Application` and calls `AddMenloApplication()` plus `MigrateDatabaseAsync()` during startup.
- `src/api/Menlo.AppHost/` provisions PostgreSQL for local development through Aspire and injects the `menlo` connection string.
- Feature code does not inject `MenloDbContext` directly. It consumes focused slice interfaces such as `IUserContext`.

## Key implementation pieces

### 1. `MenloDbContext`

`MenloDbContext` lives in `src/lib/Menlo.Application/Common/` and is the single scoped DbContext for the application.

- Applies `UseSnakeCaseNamingConvention()`
- Applies all entity configurations from the assembly
- Registers strongly typed ID value converters centrally
- Implements slice interfaces such as `IUserContext`

### 2. User persistence slice

The first persisted aggregate is `User` in the `shared.users` table.

- Slice interface: `src/lib/Menlo.Application/Auth/IUserContext.cs`
- Entity mapping: `src/lib/Menlo.Application/Auth/EntityConfigurations/UserEntityTypeConfiguration.cs`
- Migration: `src/lib/Menlo.Application/Migrations/*AddUserEntity*.cs`

`IUserContext` exposes:

```csharp
public interface IUserContext
{
    DbSet<User> Users { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

### 3. Dependency injection

`AddMenloApplication()` registers:

- `MenloDbContext`
- persistence interceptors
- slice interfaces mapped back to the scoped `MenloDbContext`

Current registration pattern:

```csharp
builder.Services.AddScoped<IUserContext>(sp => sp.GetRequiredService<MenloDbContext>());
```

### 4. Startup migration

`Menlo.Api/Program.cs` calls:

```csharp
await app.MigrateDatabaseAsync();
```

This ensures pending migrations are applied before the API begins serving traffic. A migration failure aborts startup.

## Migration workflow

Create new migrations from the repository root with:

```sh
dotnet ef migrations add <MigrationName> --project src/lib/Menlo.Application --startup-project src/api/Menlo.Api
```

The initial infrastructure migration is followed by entity-specific migrations such as `AddUserEntity`.

## Testing approach

Persistence tests live in `src/lib/Menlo.Application.Tests/` and use real PostgreSQL containers via Testcontainers.

- `Fixtures/PersistenceFixture.cs` provisions a clean PostgreSQL container and applies migrations
- `Infrastructure/MigrationSmokeTests.cs` verifies migration history and schema creation
- `Infrastructure/UserContextIntegrationTests.cs` verifies save-and-reload behavior through `IUserContext`

In-memory EF Core providers are not used.

## Validation checklist

When extending persistence:

1. Add or update the slice interface in `Menlo.Application/<BoundedContext>/`
2. Add the matching `DbSet<T>` to `MenloDbContext`
3. Add the DI registration in `AddMenloApplication()`
4. Create the migration in `Menlo.Application`
5. Add Testcontainers-backed integration coverage in `Menlo.Application.Tests`
6. Confirm the API still starts with `MigrateDatabaseAsync()`
