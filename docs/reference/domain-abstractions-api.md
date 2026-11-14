# Domain Abstractions API Reference

The core domain abstractions live in `src/lib/Menlo.Lib/Common/`. Entities and aggregates in every bounded context build on them.

The domain layer has no ASP.NET Core dependency. It never reads `ClaimsPrincipal`, `IHttpContextAccessor` or the clock. Who did something and when reach it as stamps from a factory the caller passes in. [Persistence](persistence.md) describes the factories and interceptors that do this on save.

## Core interfaces

Namespace: `Menlo.Lib.Common.Abstractions`.

### `IEntity<out TId>`

An object defined by its identity.

- `TId Id { get; }`: the entity's identifier. By convention a strongly typed ID (see [Strongly typed IDs](#strongly-typed-ids)); the type parameter has no constraint.

### `IAggregateRoot<out TId>`

A marker interface that extends `IEntity<TId>`. An aggregate root is the entry point to its aggregate; only aggregate roots are loaded and saved directly.

It does not extend `IHasDomainEvents`. Aggregates that raise events implement both.

### `IDomainEvent`

A marker interface for domain events.

### `IHasDomainEvents`

An object that collects domain events for the application layer to dispatch after persistence.

- `IReadOnlyCollection<IDomainEvent> DomainEvents { get; }`: the pending events.
- `void AddDomainEvent<TEvent>(TEvent domainEvent) where TEvent : IDomainEvent`: adds an event. The generic parameter avoids boxing at the call site.
- `void ClearDomainEvents()`: clears the pending events.

### `IAuditable`

An entity that records who created and last modified it, and when. The properties have no public setters; `Audit` is the only way to change them.

- `UserId? CreatedBy { get; }`, `DateTimeOffset? CreatedAt { get; }`: null until the first `Create` audit.
- `UserId? ModifiedBy { get; }`, `DateTimeOffset? ModifiedAt { get; }`: set by both `Create` and `Update`.
- `void Audit(IAuditStampFactory factory, AuditOperation operation)`: asks the factory for a stamp and applies it.

| Operation | Created fields | Modified fields |
| --------- | -------------- | --------------- |
| `Create`  | set            | set             |
| `Update`  | unchanged      | set             |

### `IAuditStampFactory`

Supplies the stamp for an audit.

- `AuditStamp CreateStamp()`: a stamp for the current actor and time. It always returns a stamp; there is no failure result.

### `ISoftDeletable`

An entity that is marked deleted rather than removed. Soft-deleted rows are hidden from queries by a global query filter.

- `bool IsDeleted { get; }`
- `DateTimeOffset? DeletedAt { get; }`: null unless deleted.
- `UserId? DeletedBy { get; }`: null unless deleted.
- `void Delete(ISoftDeleteStampFactory factory)`: sets all three fields from the factory's stamp.

There is no general restore operation. Where an aggregate supports undoing a delete, it does so through its own method, such as `Budget.RestoreCategory`.

### `ISoftDeleteStampFactory`

Supplies the stamp for a soft delete.

- `SoftDeleteStamp CreateStamp()`: a stamp for the current actor and time.

### `Error`

The base class for domain errors, used with `Result` from CSharpFunctionalExtensions instead of throwing for expected failures.

- `string Code { get; }`: a machine-readable code in the form `{DOMAIN}_{NUMBER}`, for example `MONEY_001`.
- `string Message { get; }`: a human-readable message.
- `protected Error(string code, string message)`: throws `ArgumentException` if either is null or whitespace.
- `ToString()` returns `[{Code}] {Message}`.

Each domain derives its own error base class (for example `MoneyError`) and a sealed class per error.

## Value objects

Namespace: `Menlo.Lib.Common.ValueObjects`.

### Strongly typed IDs

Each entity type has its own ID, a `readonly record struct` wrapping a `Guid`, so IDs of different entity types can't be mixed up at compile time. The shared ones are:

- `UserId(Guid Value)`: a person using the system.
- `HouseholdId(Guid Value)`: a household.

Both have `static NewId()`, which creates an ID from `Guid.NewGuid()`, and `ToString()`, which returns the GUID. Bounded contexts define their own IDs (`BudgetId`, `BudgetItemId` and so on) the same way. Each ID needs a value converter registered in `MenloDbContext` (see [Persistence](persistence.md)).

### `AuditStamp`

`readonly record struct AuditStamp(UserId ActorId, DateTimeOffset Timestamp, string? CorrelationId = null)`

Who performed an action and when (UTC), with an optional correlation ID for tracing.

### `SoftDeleteStamp`

`readonly record struct SoftDeleteStamp(UserId ActorId, DateTimeOffset Timestamp)`

Who soft-deleted an entity and when (UTC).

### `Money`

See [Money API](money-api.md).

## Enums

### `AuditOperation`

Namespace: `Menlo.Lib.Common.Enums`.

- `Create` (0): the first save of an entity.
- `Update` (1): a later save.

Deletes are not audit operations; they go through `ISoftDeletable.Delete`.

## Further reading

- [Tutorial: Add a Domain vertical slice](../tutorials/domain-abstractions-tutorial.md)
- [How to use Domain Abstractions](../guides/domain-abstractions-howto.md)
- [Persistence](persistence.md)
