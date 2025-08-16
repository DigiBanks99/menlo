# Requirement: Domain Auditing

## Overview

Provide a consistent, framework-agnostic auditing model for domain entities and aggregates. The domain must record who performed an operation and when it occurred, while remaining decoupled from ASP.NET and infrastructure concerns.

- Goal: Uniform create/update/delete/restore auditing across entities
- Inputs: An `IAuditStampFactory` that produces an `AuditStamp`
- Outputs: Entities with updated audit fields based on the `AuditOperation`

This requirement complements the existing domain-abstractions by defining the concrete audit semantics and the factory contract the domain consumes.

## Scope

In scope:

- Define the domain-facing audit contracts: `AuditStamp`, `AuditOperation`, and `IAuditStampFactory`
- Define how `IAuditable` entities use `IAuditStampFactory` to apply audits
- Specify which audit fields are updated for each operation
- Support soft-delete and restore semantics when present
- Allow daemon/service contexts by supporting service principal identity (e.g., MS Entra `appid`)

Out of scope:

- Direct usage of `ClaimsPrincipal`, `IHttpContextAccessor`, or `TimeProvider` in the domain
- Persistence and EF Core mapping details
- Operational logging/telemetry (handled elsewhere)

Assumptions:

- A strongly-typed `UserId` record struct exists (see Domain Abstractions requirement)
- User identity resolution is implemented via the separate UserId Resolution requirement
- The domain-abstractions define an `IAuditable` surface that calls `Audit(IAuditStampFactory, AuditOperation)`

## What gets audited

For auditable entities/aggregates, maintain the following fields (names are illustrative; exact naming is implementation-specific but must be consistent):

- CreatedBy (UserId)
- CreatedAt (DateTimeOffset, UTC)
- ModifiedBy (UserId)
- ModifiedAt (DateTimeOffset, UTC)
- DeletedBy (UserId, optional if soft-delete supported)
- DeletedAt (DateTimeOffset, UTC, optional if soft-delete supported)
- IsDeleted (bool, optional if soft-delete supported)

## When and how fields are updated

- Create
  - Set: CreatedBy, CreatedAt
  - Clear: ModifiedBy, ModifiedAt, DeletedBy, DeletedAt, IsDeleted=false
- Update
  - Set: ModifiedBy, ModifiedAt
  - Preserve: CreatedBy, CreatedAt
  - Do not touch: Deleted fields
- SoftDelete
  - Set: DeletedBy, DeletedAt, IsDeleted=true
  - Preserve: CreatedBy, CreatedAt, ModifiedBy, ModifiedAt (optional to also set ModifiedBy/At)
- Restore (from soft delete)
  - Clear: DeletedBy, DeletedAt, IsDeleted=false
  - Set: ModifiedBy, ModifiedAt

Note: If a particular entity does not support soft-delete, it should not expose the deletion fields.

## Functional Requirements

FR-1: Audit Stamp

- `AuditStamp` is an immutable value containing: `UserId` and `OccurredAt` (UTC `DateTimeOffset`).
- The stamp is produced by an injected factory at the application layer; the domain only consumes it.

FR-2: Audit Operations

- Supported operations: `Create`, `Update`, `SoftDelete`, `Restore`.
- Extending with additional operations (e.g., `Archive`, `Publish`) is allowed without breaking existing semantics.

FR-3: IAuditable Usage

- `IAuditable` exposes an operation to apply an audit: `Audit(IAuditStampFactory factory, AuditOperation operation)`.
- Implementations must update audit fields according to the table above.
- Domain code must not read from current process context; it receives the factory from the caller.

FR-4: Factory Contract

- `IAuditStampFactory.Create()` returns `Result<AuditStamp, AuditError>`.
- Factories may resolve identity from user or app contexts (e.g., `sub`/`oid`/`nameidentifier`/`appid`), or fall back to a configured system user id.
- When no identity can be resolved, `Create()` fails with a well-known error code.

FR-5: UTC and Immutability

- All timestamps are stored in UTC.
- `AuditStamp` is immutable.

FR-6: Domain Independence

- No references to `System.Security.Claims`, `IHttpContextAccessor`, or `TimeProvider` in the domain.

## Non-Functional Requirements

- Consistency: Same auditing semantics across entities.
- Performance: O(1) per audit operation; no I/O in domain layer.
- Security: No PII beyond a non-sensitive identifier (typed `UserId`).
- Testability: Deterministic via injected factory; easy to fake.

## Acceptance Criteria

- [ ] `AuditStamp`, `AuditOperation`, and `IAuditStampFactory` contracts are defined and documented.
- [ ] `IAuditable` usage rules are specified and demonstrate updating the correct fields for each operation.
- [ ] Domain contains no direct references to `System.Security.Claims` or `TimeProvider`.
- [ ] Soft-delete semantics (if supported) include `DeletedBy`, `DeletedAt`, and `IsDeleted` handling.
- [ ] Factory supports daemon contexts via `appid` or configured system user id.
- [ ] Tests cover create/update/delete/restore paths and failure when factory cannot create a stamp.

## Illustrative Contracts (non-binding)

```csharp
public readonly record struct AuditStamp(UserId UserId, DateTimeOffset OccurredAt);

public enum AuditOperation
{
    Create,
    Update,
    SoftDelete,
    Restore
}

public interface IAuditStampFactory
{
    Result<AuditStamp, AuditError> Create();
}

public interface IAuditable
{
    void Audit(IAuditStampFactory auditStampFactory, AuditOperation operation);
}
```

## Decisions

- Domain uses a factory to obtain audit stamps, avoiding direct framework dependencies.
- UTC timestamps are mandatory for consistency.
- Service principal contexts are supported by mapping `appid` (or a configured system user id) to `UserId`.

## Risks & Considerations

- Some identity providers may not provide GUIDs; mapping strategy must be configurable in the factory implementation.
- Entities without soft-delete should omit deletion fields to avoid confusion.
- Over-auditing can add noise; keep the operation set minimal and meaningful.

---

References: domain-abstractions, user-id-resolution requirements, `.github/instructions/csharp.instructions.md`, `docs/README.md`.
