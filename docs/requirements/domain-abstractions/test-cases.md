# Test Cases: Domain Model Abstractions

Link: `specifications.md`

## TC-01 Strongly-Typed Id Non-Interchangeability

- Given BudgetId and UserId are readonly record structs wrapping Guid
- When a method expects BudgetId
- Then passing a UserId does not compile (compile-time non-interchangeability)

## TC-02 Entity Contract

- Given a type implements IEntity\<BudgetId\>
- Then it exposes an Id of type BudgetId (read-only)

## TC-03 Aggregate Root Marker

- Given a type implements IAggregateRoot\<BudgetId\>
- Then it is also an IEntity\<BudgetId\> and compiles as a marker

## TC-04 Domain Events Collection

- Given an entity implements IHasDomainEvents
- When AddDomainEvent is called twice and ClearDomainEvents is called
- Then the collection first has 2 events and then becomes empty

## TC-05 Audit Create Operation

- Given an IAuditable instance with null CreatedBy/CreatedAt and a fake IAuditStampFactory returning a fixed AuditStamp
- When Audit(factory, Create) is called
- Then CreatedBy/CreatedAt and ModifiedBy/ModifiedAt are set to the factory-provided stamp values

## TC-06 Audit Update Operation

- Given an IAuditable instance with existing CreatedBy/CreatedAt and a fake IAuditStampFactory returning a new stamp
- When Audit(factory, Update) is called
- Then only ModifiedBy/ModifiedAt change, Created fields remain unchanged

## TC-07 No ClaimsPrincipal in Domain

- Search the domain abstractions for System.Security.Claims
- Expect no references found

## TC-08 Result and Error Guidance

- Given a domain method contract using Result
- Then invalid inputs are represented via Result.Failure with an Error code and message
