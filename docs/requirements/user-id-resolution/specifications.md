# Requirement: UserId Resolution via IoC (from ClaimsPrincipal)

## Overview

Provide a decoupled mechanism to resolve a strongly-typed `UserId` for the current request context using inversion of control. The domain model must remain independent of ASP.NET; the adapter lives in the application/infrastructure layer.

- Goal: Consistent, testable, framework-agnostic user identity resolution
- Inputs: ASP.NET `ClaimsPrincipal` (or equivalent)
- Outputs: `UserId` for the domain layer

## Scope

In scope:

- Define `IUserContextProvider` (or `IUserContext`) abstraction providing `UserId` (strongly-typed)
- Provide an ASP.NET Core implementation that maps `ClaimsPrincipal` → `UserId`
- Support configuration of claim types / mapping strategy
- Error handling for missing/invalid identity

Out of scope:

- Domain references to `ClaimsPrincipal` (must not occur)
- Persistence, concrete domain logic, or EF Core mappings
- Multi-tenant scoping beyond providing a `UserId`

Assumptions:

- Strongly-typed `UserId` record struct exists per domain abstractions requirement
- Application uses ASP.NET Core authentication

## Functional Requirements

FR-1: Current User Abstraction

- Expose `GetUserId()` methods with a CSharpFunctionExtension result
- Support async if needed, but default should be sync for in-memory claims

FR-2: Claims Mapping Strategy

- Default claim types: `sub`, `oid`, or `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier`
- Allow override via configuration
- Validate GUID format (or custom format if alternate `UserId` type later)

FR-3: Domain Decoupling

- No domain references to `ClaimsPrincipal`; only `UserId` (strongly-typed) flows into domain

FR-4: Error Handling

- If no valid user id claim is present, return a failure with a clear error code (e.g., `user.missing-id-claim`)
- Provide both exception and Result-based pathways (application choice)
- Use strongly typed errors

FR-5: Testability

- Abstractions easily mocked; implementation covered by unit/integration tests

## Non-Functional Requirements

- Decoupled: Domain receives only `UserId`, never `ClaimsPrincipal`
- Secure-by-default: Fail closed when identity missing; explicit opt-in for anonymous contexts
- Observability: Minimal logs on mapping failures, no PII beyond a non-sensitive identifier

## Acceptance Criteria

- [ ] `IUserContextProvider` (or `IUserContext`) abstraction is defined
- [ ] ASP.NET Core implementation maps `ClaimsPrincipal` → `UserId` with configurable claim precedence
- [ ] Missing/invalid claim yields a clear failure (code + message)
- [ ] No references to `System.Security.Claims` in domain abstractions
- [ ] Tests demonstrate mapping for `sub`, `oid`, and `nameidentifier` claims and error cases

## Illustrative Interfaces (non-binding)

```csharp
public interface IUserContextProvider
{
    Result<UserId, UserError> GetUserId();
}
```

## Decisions

- Use IoC to inject both the current-user abstraction and the time source
- Make claim precedence configurable; default to `sub` then `oid` then `nameidentifier`
- Use Result-based failures

## Risks & Considerations

- Diverse identity providers may format IDs differently; prefer stable GUIDs for `UserId`
- Anonymous/daemon contexts need an explicit strategy (e.g., system user id) if required in future

---

References: domain-abstractions requirement, `.github/instructions/csharp.instructions.md`, `docs/README.md`.
