# Test Cases: Domain Auditing

Link: `specifications.md`

## TC-01 Create applies Created* only

- Given a new auditable entity
- And the factory returns AuditStamp(U1, T1)
- When Audit(..., Create) is applied
- Then CreatedBy=U1, CreatedAt=T1
- And Modified*, Deleted* are null/default, IsDeleted=false

## TC-02 Update applies Modified\* and preserves Created\*

- Given an entity with CreatedBy=U1, CreatedAt=T1
- And the factory returns AuditStamp(U2, T2)
- When Audit(..., Update) is applied
- Then ModifiedBy=U2, ModifiedAt=T2
- And CreatedBy=U1, CreatedAt=T1 remain unchanged

## TC-03 SoftDelete sets Deleted* and IsDeleted

- Given an existing entity
- And the factory returns AuditStamp(U3, T3)
- When Audit(..., SoftDelete) is applied
- Then DeletedBy=U3, DeletedAt=T3, IsDeleted=true

## TC-04 Restore clears Deleted\* and sets Modified\*

- Given an entity with IsDeleted=true, DeletedBy=U3, DeletedAt=T3
- And the factory returns AuditStamp(U4, T4)
- When Audit(..., Restore) is applied
- Then DeletedBy/DeletedAt cleared, IsDeleted=false
- And ModifiedBy=U4, ModifiedAt=T4

## TC-05 Factory failure bubbles up

- Given the factory returns failure `audit.identity-missing`
- When Audit(...) is invoked
- Then the operation is prevented or a domain error is surfaced (per implementation), and no fields change

## TC-06 Domain decoupling from framework

- Search domain for references to `System.Security.Claims` and `TimeProvider`
- Expect none

## TC-07 Daemon contexts supported

- Given UserId resolution via service principal `appid`
- And the factory provides AuditStamp(AppId, T)
- When any operation audit is applied
- Then audit fields record AppId in *By properties
