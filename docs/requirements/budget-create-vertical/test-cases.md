# Test Cases: Create Budget (Vertical Slice)

## UI

- Given invalid fields When typing Then the Create button remains disabled
- Given valid fields When submitting Then POST is sent and success path triggers navigation
- Given 409 from API When submitting Then the error shows inline next to Name/Period

## API (Integration)

- Given valid payload When POST /api/budgets Then 201 with Location and body containing id
- Given missing/invalid fields When POST Then 400 with field errors
- Given duplicate (same UserId+Year+Month+Name) When POST Then 409 with code DuplicateBudget
- Given unauthenticated When POST Then 401; unauthorised roles When POST Then 403
- Given owned budget When GET /api/budgets/{id} Then 200 with expected DTO
- Given other user’s budget When GET Then 404

## Persistence

- Unique index enforced for (UserId, Year, Month, Name)
- Data stored and retrievable via projection; status=Draft

## Observability

- Trace spans include UI → API → DB; logs contain create operation markers

## Traceability

- Maps to `specifications.md` and `implementation.md`
