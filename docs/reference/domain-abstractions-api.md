# Domain Abstractions API Reference

This document provides a reference for the core domain abstractions used in the Menlo project. These abstractions are located in the `Menlo.Lib.Common` namespace and form the building blocks for
Domain-Driven Design (DDD) implementation.

## Core Interfaces

### `IEntity<TId>`

Defines the contract for a Domain Entity. Entities are objects that are defined by their identity rather than their attributes.

**Namespace:** `Menlo.Lib.Common.Abstractions`

**Type Parameters:**

- `TId`: The type of the entity's identifier. Must be a `struct`.

**Properties:**

- `TId Id { get; }`: The unique identifier for the entity.

---

### `IAggregateRoot<TId>`

Defines the contract for an Aggregate Root. An Aggregate Root is an Entity that binds together a cluster of associated objects (the Aggregate) and serves as the entry point for all access to that cluster.

**Namespace:** `Menlo.Lib.Common.Abstractions`

**Inherits from:** `IEntity<TId>`, `IHasDomainEvents`

**Type Parameters:**

- `TId`: The type of the aggregate root's identifier. Must be a `struct`.

---

### `IDomainEvent`

A marker interface for Domain Events. Domain Events represent something meaningful that happened in the domain.

**Namespace:** `Menlo.Lib.Common.Abstractions`

---

### `IHasDomainEvents`

Defines the contract for an object that can raise domain events. This is typically implemented by Aggregate Roots.

**Namespace:** `Menlo.Lib.Common.Abstractions`

**Methods:**

- `void AddDomainEvent<TEvent>(TEvent domainEvent) where TEvent : IDomainEvent`: Adds a domain event to the object's collection of pending events.
- `IReadOnlyCollection<IDomainEvent> DomainEvents { get; }`: Gets the collection of pending domain events.
- `void ClearDomainEvents()`: Clears all pending domain events.

**Note:** The `AddDomainEvent` method is generic to avoid boxing of struct-based domain events when adding them to the internal collection (though they are stored as `IDomainEvent`).

---

### `IAuditable`

Defines the contract for entities that require auditing of creation and modification.

**Namespace:** `Menlo.Lib.Common.Abstractions`

**Properties:**

- `UserId? CreatedBy { get; }`: The user who created the entity.
- `DateTimeOffset? CreatedAt { get; }`: The timestamp when the entity was created.
- `UserId? ModifiedBy { get; }`: The user who last modified the entity.
- `DateTimeOffset? ModifiedAt { get; }`: The timestamp when the entity was last modified.

**Methods:**

- `void Audit(IAuditStampFactory factory, AuditOperation operation)`: Updates the audit fields based on the operation type.

---

### `IAuditStampFactory`

Defines the contract for a factory that creates `AuditStamp` instances. This allows for abstraction of the current user and time provider.

**Namespace:** `Menlo.Lib.Common.Abstractions`

**Methods:**

- `AuditStamp CreateStamp()`: Creates a new audit stamp for the current user and time.

---

## Base Classes

### `Error`

Represents a domain error. This class is used in conjunction with the Result pattern to return typed errors instead of throwing exceptions for expected failure scenarios.

**Namespace:** `Menlo.Lib.Common.Abstractions`

**Properties:**

- `string Code { get; }`: A unique code for the error.
- `string Message { get; }`: A human-readable description of the error.

**Constructors:**

- `protected Error(string code, string message)`: Initializes a new instance of the `Error` class.

---

## Value Objects

### `UserId`

A strongly-typed identifier for a user. Implemented as a `readonly record struct` for performance and immutability.

**Namespace:** `Menlo.Lib.Common.ValueObjects`

**Properties:**

- `Guid Value { get; }`: The underlying GUID value.

**Constructors:**

- `public UserId(Guid value)`: Initializes a new instance of `UserId`.

**Static Methods:**

- `public static UserId New()`: Creates a new `UserId` with a generated GUID.
- `public static UserId Empty`: Returns an empty `UserId`.

---

### `AuditStamp`

Represents an audit record containing the user ID, timestamp, and operation type. Implemented as a `readonly record struct`.

**Namespace:** `Menlo.Lib.Common.ValueObjects`

**Properties:**

- `UserId ActorId { get; }`: The ID of the user who performed the operation.
- `DateTimeOffset Timestamp { get; }`: The timestamp when the operation occurred.
- `string? CorrelationId { get; }`: Optional correlation ID for distributed tracing.

---

## Enums

### `AuditOperation`

Enumerates the types of audit operations.

**Namespace:** `Menlo.Lib.Common.Enums`

**Values:**

- `Create`: Represents a creation operation.
- `Update`: Represents an update operation.
- `Delete`: Represents a deletion operation.

## Further Reading

- [Tutorial: Add a Domain vertical slice](../tutorials/domain-abstractions-tutorial.md)
- [How to use Domain Abstractions](../guides/domain-abstractions-howto.md)
