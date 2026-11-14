# Persistence

Menlo stores its data in PostgreSQL through Entity Framework Core. The database runs on the home server next to the API, never in a cloud-managed service, and only the API talks to it.

To add an entity, configuration, migration or test, follow the `menlo-persistence` skill (`.claude/skills/menlo-persistence/`). This page describes the as-built pieces.

## Projects

- `src/lib/Menlo.Application/` owns EF Core: the DbContext, entity configurations, value converters, interceptors, migrations and slice interfaces.
- `src/api/Menlo.Api/` calls `AddMenloApplication()` at startup and `MigrateDatabaseAsync()` before serving traffic.
- `src/api/Menlo.AppHost/` provisions PostgreSQL for local development with Aspire and supplies the `menlo` connection string.

## `MenloDbContext`

`Menlo.Application.Common.MenloDbContext` is the only DbContext. It:

- applies every `IEntityTypeConfiguration` in the assembly,
- uses snake_case names (`UseSnakeCaseNamingConvention()`),
- registers a value converter for each strongly typed ID in `ConfigureConventions`, so IDs are stored as `uuid`,
- adds an `IsDeleted == false` query filter to every `ISoftDeletable` entity type,
- implements the slice interfaces.

### Slice interfaces

Feature code never takes `MenloDbContext` directly. Each bounded context has a slice interface that exposes only its own `DbSet`s and `SaveChangesAsync`, and `AddMenloApplication()` maps each one to the scoped `MenloDbContext`:

| Interface             | Sets                                                          |
| --------------------- | ------------------------------------------------------------- |
| `IUserContext`        | `Users`                                                       |
| `IHouseholdContext`   | `Households`                                                  |
| `IBudgetContext`      | `Budgets`, `BudgetCategories`, `CanonicalCategories`, `BudgetItems` |
| `IOnboardingContext`  | `OnboardingStates`                                            |

### Schemas

Tables are grouped by bounded context:

| Schema          | Tables                                                           |
| --------------- | ---------------------------------------------------------------- |
| `shared`        | `users`, `households`, `onboarding_states`                       |
| `budget_schema` | `budgets`, `budget_categories`, `canonical_categories`, `budget_items` |

New bounded contexts get their own schema.

## Column conventions

- **Money** is mapped as an EF Core complex property: a `numeric` amount column and a currency column of at most 3 characters. Never store money as a floating-point type. Today a required amount is `numeric(18,2)`, matching `Money`'s two-decimal rounding; the optional amounts on `BudgetItem` are `numeric(18,4)`.
- **Timestamps** are `DateTimeOffset` values in UTC.
- **IDs** are `uuid`, through the value converters.

## Auditing and soft delete

Two `SaveChangesInterceptor`s apply the domain contracts from [Domain Abstractions](domain-abstractions-api.md) on every save, so feature code never sets audit or delete fields itself.

- `AuditingInterceptor` calls `Audit(factory, AuditOperation.Create)` on each added `IAuditable` entity, and `Audit(factory, AuditOperation.Update)` on each modified one.
- `SoftDeleteInterceptor` turns each deleted `ISoftDeletable` entity into a modification and calls `Delete(factory)` on it, so the row stays in the table.

The auditing interceptor runs first, so a soft delete sets the deleted fields and leaves the modified fields as they were.

### Who the actor is

`ReflectiveAuditStampFactory` implements both `IAuditStampFactory` and `ISoftDeleteStampFactory`. It reads the current user's `ClaimTypes.NameIdentifier` claim, or `sub` if that is missing, and uses it as the `UserId` when it parses as a GUID. Without an HTTP user, for example during seeding or a background job, the actor is the system user, `UserId(Guid.Empty)`. The timestamp is `DateTimeOffset.UtcNow`.

The factory resolves `IHttpContextAccessor` by reflection, so `Menlo.Application` doesn't need a compile-time reference to ASP.NET Core.

## Connections

`AddMenloApplication()` reads the `menlo` connection string and fails at startup if it is missing. Npgsql pools connections. If the connection string doesn't set an SSL mode, the API sets one:

- `Disable` in Development, or when the host is `localhost`, `127.0.0.1` or `::1`,
- `Require` otherwise.

## Migrations

Migrations live in `src/lib/Menlo.Application/Migrations/`. `MigrateDatabaseAsync()` applies pending migrations at API startup, and a failed migration stops the API from starting. Set `Menlo:SkipMigration` to `true` to skip it.

Create a migration from the repository root:

```sh
dotnet ef migrations add <MigrationName> --project src/lib/Menlo.Application --startup-project src/api/Menlo.Api
```

## Tests

Persistence tests in `src/lib/Menlo.Application.Tests/` run against real PostgreSQL containers through Testcontainers; the EF Core in-memory provider isn't used. See the [Testing guide](../guides/testing-howto.md).
