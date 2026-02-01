# Implementation Plan: In-Memory to TestContainers Migration

## Overview

This document provides a step-by-step technical plan for migrating the `Menlo.Api.Tests` project from using the `Microsoft.EntityFrameworkCore.InMemory` provider to `Testcontainers.PostgreSql` with `Respawn` for database state management.

## Prerequisites

- Docker engine must be available in the development environment (already configured in devcontainer)
- CI/CD pipeline must support Docker (GitHub Actions with Ubuntu runners)
- Understanding of XUnit fixtures and `IAsyncLifetime`

## Phase 1: Package Management Updates

### 1.1 Update Directory.Packages.props

Add the following package versions to central package management:

```xml
<!-- TestContainers for integration tests -->
<PackageVersion Include="Testcontainers.PostgreSql" Version="4.3.0" />
<PackageVersion Include="Respawn" Version="6.2.1" />
```

### 1.2 Update Menlo.Api.Tests.csproj

**Remove:**
```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" />
```

**Add:**
```xml
<PackageReference Include="Testcontainers.PostgreSql" />
<PackageReference Include="Respawn" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" />
```

## Phase 2: Create Shared PostgreSQL Container Fixture

### 2.1 Create PostgresContainerFixture

Create a new fixture class that manages the PostgreSQL container lifecycle. This fixture will be shared across test collections to minimise container startup overhead.

**File:** `src/api/Menlo.Api.Tests/Persistence/Fixtures/PostgresContainerFixture.cs`

**Pattern to follow:**

```csharp
using Testcontainers.PostgreSql;

namespace Menlo.Api.Tests.Persistence.Fixtures;

/// <summary>
/// XUnit fixture that manages the lifecycle of a PostgreSQL container.
/// Shared across test collections to minimise startup time.
/// </summary>
public sealed class PostgresContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;

    public PostgresContainerFixture()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("menlo_tests")
            .WithUsername("test_user")
            .WithPassword("test_password")
            .Build();
    }

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}
```

### 2.2 Create Collection Definition

Create a collection definition to share the container across tests.

**File:** `src/api/Menlo.Api.Tests/Persistence/Fixtures/PostgresTestCollection.cs`

```csharp
namespace Menlo.Api.Tests.Persistence.Fixtures;

[CollectionDefinition(Name)]
public class PostgresTestCollection : ICollectionFixture<PostgresContainerFixture>
{
    public const string Name = "PostgreSQL";
}
```

## Phase 3: Create Database Initialisation Helper

### 3.1 Create DbContextFactory Helper

Create a factory that produces `MenloDbContext` instances configured for PostgreSQL.

**File:** `src/api/Menlo.Api.Tests/Persistence/Fixtures/TestDbContextFactory.cs`

```csharp
using Menlo.Api.Persistence.Data;
using Menlo.Api.Persistence.Interceptors;
using Menlo.Lib.Common.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Menlo.Api.Tests.Persistence.Fixtures;

/// <summary>
/// Factory for creating MenloDbContext instances for tests.
/// </summary>
public static class TestDbContextFactory
{
    public static MenloDbContext Create(
        string connectionString,
        IAuditStampFactory? auditStampFactory = null,
        bool applyMigrations = true)
    {
        DbContextOptionsBuilder<MenloDbContext> optionsBuilder = new DbContextOptionsBuilder<MenloDbContext>()
            .UseNpgsql(connectionString)
            .EnableSensitiveDataLogging();

        if (auditStampFactory is not null)
        {
            optionsBuilder.AddInterceptors(
                new AuditingInterceptor(auditStampFactory),
                new SoftDeleteInterceptor(auditStampFactory));
        }

        MenloDbContext context = new(optionsBuilder.Options);

        if (applyMigrations)
        {
            context.Database.EnsureCreated();
        }

        return context;
    }
}
```

## Phase 4: Integrate Respawn for State Reset

### 4.1 Create Respawn Helper

Create a helper class to reset database state between tests.

**File:** `src/api/Menlo.Api.Tests/Persistence/Fixtures/DatabaseRespawner.cs`

```csharp
using Npgsql;
using Respawn;

namespace Menlo.Api.Tests.Persistence.Fixtures;

/// <summary>
/// Helper class to reset database state between tests using Respawn.
/// </summary>
public sealed class DatabaseRespawner
{
    private Respawner? _respawner;
    private readonly string _connectionString;

    public DatabaseRespawner(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task InitialiseAsync()
    {
        await using NpgsqlConnection connection = new(_connectionString);
        await connection.OpenAsync();

        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],
            // Exclude EF migrations history table
            TablesToIgnore = ["__EFMigrationsHistory"]
        });
    }

    public async Task ResetAsync()
    {
        if (_respawner is null)
        {
            throw new InvalidOperationException("Respawner not initialised. Call InitialiseAsync first.");
        }

        await using NpgsqlConnection connection = new(_connectionString);
        await connection.OpenAsync();
        await _respawner.ResetAsync(connection);
    }
}
```

## Phase 5: Refactor Existing Test Files

### 5.1 Refactor DbContextFixture

Update the existing `DbContextFixture` to use PostgreSQL instead of InMemory.

**Current location:** `src/api/Menlo.Api.Tests/Persistence/Fixtures/DbContextFixture.cs`

**Changes required:**
1. Implement `IAsyncLifetime` instead of `IDisposable`
2. Accept `PostgresContainerFixture` via constructor injection
3. Replace `UseInMemoryDatabase` with `UseNpgsql`
4. Add Respawn integration for state reset
5. Keep the mock `IAuditStampFactory` logic intact

### 5.2 Refactor EntityConfigurationTests

**File:** `src/api/Menlo.Api.Tests/Persistence/Configurations/EntityConfigurationTests.cs`

**Changes required:**
1. Add `[Collection(PostgresTestCollection.Name)]` attribute
2. Implement `IAsyncLifetime` instead of `IDisposable`
3. Inject `PostgresContainerFixture` via constructor
4. Replace `UseInMemoryDatabase` with `UseNpgsql(fixture.ConnectionString)`
5. Add Respawn reset in `InitializeAsync` or after each test

### 5.3 Refactor AuditingInterceptorTests

**File:** `src/api/Menlo.Api.Tests/Persistence/Interceptors/AuditingInterceptorTests.cs`

**Changes required:**
1. Add `[Collection(PostgresTestCollection.Name)]` attribute
2. Implement `IAsyncLifetime`
3. Inject `PostgresContainerFixture`
4. Replace `UseInMemoryDatabase` with PostgreSQL connection
5. Add database reset between tests

### 5.4 Refactor SoftDeleteInterceptorTests

**File:** `src/api/Menlo.Api.Tests/Persistence/Interceptors/SoftDeleteInterceptorTests.cs`

**Changes required:**
1. Add `[Collection(PostgresTestCollection.Name)]` attribute
2. Implement `IAsyncLifetime`
3. Inject `PostgresContainerFixture`
4. Replace `UseInMemoryDatabase` with PostgreSQL connection
5. Add database reset between tests

## Phase 6: CI/CD Considerations

### 6.1 GitHub Actions Configuration

The existing CI workflow should work without changes because:
- GitHub-hosted runners include Docker
- The devcontainer already has Docker configured

### 6.2 Container Startup Optimisation

To minimise test execution time:
1. Use `postgres:16-alpine` image (smaller download)
2. Share the container across test collections (single startup per test run)
3. Use Respawn for fast state reset (faster than container restart)

### 6.3 Timeout Configuration

Add timeout configuration to prevent hanging tests:

```csharp
_container = new PostgreSqlBuilder()
    .WithStartupCallback((container, ct) =>
    {
        // Log container startup for debugging CI issues
        Console.WriteLine($"PostgreSQL container started: {container.Id}");
        return Task.CompletedTask;
    })
    .WithWaitStrategy(Wait.ForUnixContainer()
        .UntilPortIsAvailable(5432))
    .Build();
```

## Phase 7: Verification Steps

### 7.1 Local Verification

1. Run `dotnet build src/api/Menlo.Api.Tests/Menlo.Api.Tests.csproj`
2. Run `dotnet test src/api/Menlo.Api.Tests/Menlo.Api.Tests.csproj`
3. Verify all tests pass
4. Check console output for container startup messages

### 7.2 Solution-Wide Verification

1. Search for `UseInMemoryDatabase` - should return no matches in test files
2. Search for `Microsoft.EntityFrameworkCore.InMemory` - should not be referenced
3. Run `dotnet build` for entire solution
4. Run all tests via `dotnet test`

### 7.3 CI Pipeline Verification

1. Push changes to a feature branch
2. Verify CI workflow passes
3. Check test execution time increase is acceptable (< 2x baseline)

## Rollback Strategy

If issues are encountered during migration:

1. **Immediate rollback:** Revert all changes and restore InMemory references
2. **Partial rollback:** Keep new fixtures but allow both InMemory and PostgreSQL tests during transition
3. **Forward fix:** Address specific test failures while keeping PostgreSQL infrastructure

## File Change Summary

| File | Action | Description |
|------|--------|-------------|
| `Directory.Packages.props` | Modify | Add Testcontainers.PostgreSql and Respawn versions |
| `Menlo.Api.Tests.csproj` | Modify | Remove InMemory, add new packages |
| `PostgresContainerFixture.cs` | Create | New fixture for container lifecycle |
| `PostgresTestCollection.cs` | Create | Collection definition for shared fixture |
| `TestDbContextFactory.cs` | Create | Factory for creating test DbContext |
| `DatabaseRespawner.cs` | Create | Helper for Respawn integration |
| `DbContextFixture.cs` | Modify | Refactor to use PostgreSQL |
| `EntityConfigurationTests.cs` | Modify | Update to use PostgreSQL fixture |
| `AuditingInterceptorTests.cs` | Modify | Update to use PostgreSQL fixture |
| `SoftDeleteInterceptorTests.cs` | Modify | Update to use PostgreSQL fixture |

## Estimated Effort

| Phase | Estimated Time |
|-------|---------------|
| Phase 1: Package Management | 15 minutes |
| Phase 2: Container Fixture | 30 minutes |
| Phase 3: DbContext Factory | 20 minutes |
| Phase 4: Respawn Integration | 30 minutes |
| Phase 5: Test Refactoring | 2 hours |
| Phase 6: CI/CD Validation | 30 minutes |
| Phase 7: Verification | 30 minutes |
| **Total** | **~4 hours** |

## References

- [Testcontainers for .NET Documentation](https://testcontainers.com/guides/getting-started-with-testcontainers-for-dotnet/)
- [Respawn GitHub Repository](https://github.com/jbogard/Respawn)
- [XUnit Collection Fixtures](https://xunit.net/docs/shared-context#collection-fixture)
- [Project C# Instructions](../../../.github/instructions/csharp.instructions.md) - Testing section
