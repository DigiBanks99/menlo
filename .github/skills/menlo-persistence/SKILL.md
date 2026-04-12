---
name: menlo-persistence
description: Implement EF Core + PostgreSQL persistence for the Menlo home management app. Use this skill whenever you are adding new entities, bounded-context slices, entity type configurations, slice context interfaces, interceptors, migrations, or integration tests to the Menlo codebase. Also use it when modifying MenloDbContext, ISoftDeletable implementations, or anything in Menlo.Application that touches the database. If the user is working on data access, a new feature slice, asking how data should be stored, or asking about testing persistence — this skill has the answers and must be followed precisely. Even if the user just says "add an entity" or "make this persistable", invoke this skill.
---

# Menlo Persistence Layer

Everything database-related in Menlo lives in `src/lib/Menlo.Application/`. This is the only project that references EF Core directly. `Menlo.Api` calls `AddMenloApplication()` and knows nothing about EF Core internals.

## Project Layout

```
src/lib/Menlo.Application/
├── Common/
│   ├── MenloDbContext.cs                 ← Single DbContext; implements ALL slice interfaces
│   ├── Interceptors/
│   │   ├── AuditingInterceptor.cs
│   │   └── SoftDeleteInterceptor.cs
│   └── ServiceCollectionExtensions.cs   ← AddMenloApplication()
└── <BoundedContext>/                     ← One folder per bounded context (Auth, Budget, etc.)
    ├── I<BoundedContext>Context.cs       ← Slice interface (ONLY this context's DbSets)
    └── EntityConfigurations/
        └── <Entity>EntityTypeConfiguration.cs
```

PostgreSQL schemas: `shared` (User, cross-cutting), `planning`, `budget`, `financial`, `events`, `household`.

## The Slice Interface Pattern — the most important convention

Feature handlers **never** inject `MenloDbContext` directly. Each bounded context gets a focused interface that exposes only its own `DbSet<T>` properties:

```csharp
// Menlo.Application/Auth/IUserContext.cs
public interface IUserContext
{
    DbSet<User> Users { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

`MenloDbContext` implements **all** slice interfaces. DI maps each interface back to the same scoped `MenloDbContext` instance, so change tracking is shared within a request.

**When adding a new bounded context (e.g., Budget):**
1. Create `Menlo.Application/Budget/IBudgetContext.cs` with only Budget-related `DbSet<T>` properties
2. Add those `DbSet<T>` properties to `MenloDbContext` and declare it implements `IBudgetContext`
3. Register: `services.AddScoped<IBudgetContext>(sp => sp.GetRequiredService<MenloDbContext>())`
4. Feature handlers inject `IBudgetContext`, never `MenloDbContext`

## Entity Type Configurations

Each entity gets its own `IEntityTypeConfiguration<T>` class. Keep them small — most mapping is automatic:

```csharp
// Menlo.Application/Auth/EntityConfigurations/UserEntityTypeConfiguration.cs
public sealed class UserEntityTypeConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users", "shared");
        // Column names: automatic snake_case (UseSnakeCaseNamingConvention)
        // Typed IDs → uuid: automatic central value converter
        // DateTimeOffset → timestamptz: Npgsql default, no config needed
        // Decimal money values need explicit type:
        // builder.Property(x => x.Amount).HasColumnType("numeric(18,4)");
    }
}
```

Register all configurations at once in `OnModelCreating`:
```csharp
modelBuilder.ApplyConfigurationsFromAssembly(typeof(MenloDbContext).Assembly);
```

## Column Type Rules

| C# type | PostgreSQL column type | How it's configured |
|---|---|---|
| `decimal` / `Money` value object | `numeric(18,4)` | **Explicit**: `HasColumnType("numeric(18,4)")` |
| `DateTimeOffset` | `timestamptz` | **Automatic**: Npgsql default — no config needed |
| Strongly typed ID (e.g., `UserId`, `BudgetId`) | `uuid` | **Automatic**: central value converter scans for GuidValueObject types |
| `string` (unconstrained) | `text` | **Automatic**: Npgsql default |
| `bool` | `boolean` | **Automatic**: Npgsql default |

**Never** specify column names manually (e.g., `HasColumnName("user_id")`). The snake_case convention handles it.

## `ISoftDeletable` — Soft Deletes

Every user-generated entity implements `ISoftDeletable` from `Menlo.Lib.Common.Abstractions`:

```csharp
public interface ISoftDeletable
{
    bool IsDeleted { get; }
    DateTimeOffset? DeletedAt { get; }
    UserId? DeletedBy { get; }
}
```

Two things make soft deletes work automatically:
- **`SoftDeleteInterceptor`**: Intercepts `EntityState.Deleted`, changes it to `EntityState.Modified`, stamps `IsDeleted = true`, `DeletedAt`, `DeletedBy` via `ISoftDeleteStampFactory`
- **Global query filter**: Registered in `OnModelCreating` for every `ISoftDeletable` type — excludes `IsDeleted = true` from all queries automatically

Callers **never** set `IsDeleted`, `DeletedAt`, or `DeletedBy` manually. Use `.Remove()` on the DbSet and the interceptor does the rest.

To access soft-deleted records (admin/restore): `.IgnoreQueryFilters()`

Migration columns for any `ISoftDeletable` entity: `is_deleted boolean NOT NULL DEFAULT false`, `deleted_at timestamptz NULL`, `deleted_by uuid NULL`.

## `IAuditable` — Auditing

Every entity implements `IAuditable` from `Menlo.Lib.Common.Abstractions`. The `AuditingInterceptor` automatically calls `.Audit(factory, AuditOperation.Create)` on `EntityState.Added` and `.Audit(factory, AuditOperation.Update)` on `EntityState.Modified` — callers never touch audit fields directly.

## Adding a New Entity — Step-by-Step Checklist

1. **Domain model** (in `Menlo.Lib/<BoundedContext>/Entities/`): entity class implementing `IEntity<TId>`, `IAggregateRoot<TId>`, `IAuditable`, `ISoftDeletable`, `IHasDomainEvents`
2. **Strongly typed ID** (in `Menlo.Lib/<BoundedContext>/ValueObjects/`): `readonly record struct <Entity>Id(Guid Value)` — follows the `UserId` pattern
3. **Entity type configuration** (in `Menlo.Application/<BoundedContext>/EntityConfigurations/`): `builder.ToTable("<table>", "<schema>")` + decimal column types
4. **`DbSet<T>`** added to `MenloDbContext`
5. **Slice interface** (in `Menlo.Application/<BoundedContext>/`): expose the `DbSet<T>` + `SaveChangesAsync`; `MenloDbContext` implements it
6. **DI registration** in `AddMenloApplication()`: `services.AddScoped<I<Entity>Context>(sp => sp.GetRequiredService<MenloDbContext>())`
7. **Migration**: `dotnet ef migrations add Add<Entity> --project src/lib/Menlo.Application --startup-project src/api/Menlo.Api`
8. **Integration test** in `Menlo.Application.Tests` (see Testing section below)

## Migrations

Always use these exact flags:
```sh
dotnet ef migrations add <MigrationName> \
  --project src/lib/Menlo.Application \
  --startup-project src/api/Menlo.Api
```

Migrations live in `Menlo.Application/Common/Migrations/` (or the default EF Core output path).

## Testing Rules — Non-Negotiable

**No in-memory EF Core. Ever.** In-memory providers don't enforce constraints, column types, or SQL semantics. Every persistence test must use real PostgreSQL via TestContainers.

**Test external behaviour, not internal wiring:**
- ✅ Save entity → retrieve it → assert fields are correct
- ✅ Delete entity → assert not returned by standard query
- ✅ Delete entity → assert returned by `.IgnoreQueryFilters()` with `IsDeleted = true`
- ✅ Create/update entity → assert audit fields populated
- ❌ Assert that `Audit()` was called
- ❌ Assert that a value converter was registered

**Minimal test fixture pattern:**
```csharp
var container = new PostgreSqlBuilder().Build();
await container.StartAsync();
var services = new ServiceCollection();
services.AddMenloApplication(container.GetConnectionString());
var provider = services.BuildServiceProvider();
await provider.GetRequiredService<MenloDbContext>().Database.MigrateAsync();
```

For full code examples of the DbContext skeleton, DI registration, and test fixtures, read `references/patterns.md`.
