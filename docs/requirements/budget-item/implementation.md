# Implementation Plan: Add Budget Item from UI (Budget Domain)

## Summary

Implement a vertical slice to add a Planned allocation on a leaf category, persist it, and visualise the hierarchy for a selected month with Planned vs Spent. Align with DDD and repository standards in `.github/instructions/csharp.instructions.md` and Angular guidelines.

## Steps

1. Domain & Data

- Add command/use case: `AddBudgetAllocation` with inputs: BudgetId, Year, Month, CategoryId, Money, Attribution, Notes
- Enforce invariants: leaf-only, attribution sum=100, amount>0, month-in-year
- Emit domain event: `BudgetAllocationPlanned`
- Persistence: create table/entity for `BudgetAllocation` keyed by (BudgetId, CategoryId, Year, Month, AllocationId)
- Indexing: (BudgetId, Year, Month), (CategoryId), include Amount

1. API (Minimal APIs)

- POST `api/budgets/{budgetId}/allocations` (kebab-case); returns 201 with location
- GET `api/budgets/{budgetId}/tree?year=&month=` returns hierarchical read model with totals
- Validation: return 400 with domain error codes/messages; map to problem+json

1. Read Model & Aggregation

- Query allocations and transactions for month
- Build tree with roll-ups: node.planned = sum(children or leaf), node.spent = sum(children or leaf)
- Consider server-side aggregation for performance

1. UI (Angular)

- Page: Budget visualiser (month selector, filters)
- Dialog/form: Add allocation (leaf category selector, amount, attribution controls)
- State: Signals for selected month, filters, and data; error signals for validation
- Visuals: Tree with progress bars (Spent vs Planned), over-budget highlighting, totals per node; Realized layer hidden when none exist
- A11y: ARIA tree role, labelled progress, keyboard navigation

1. Telemetry & Feature Toggle

- Feature flag: `budget.add-allocation` gating UI entry points
- Track allocation-created and view-loaded events (without PII)

1. Testing

- Unit tests for domain rules (leaf-only, attribution sum, etc.)
- API tests (201/400 paths, OpenAPI responses)
- UI component tests for rendering, filters, a11y basics

## Interfaces (Sketch)

-- POST body

```json
{
  "budgetId": "<guid>",
  "year": 2025,
  "month": 8,
  "categoryId": "<guid>",
  "amount": 1250.00,
  "currency": "ZAR",
  "notes": "Groceries top-up",
  "attribution": [ { "type": "Family", "percent": 100 } ]
}
```

-- GET response (excerpt)

```json
{
  "tree": [
    { "categoryId": "...", "name": "Housing", "planned": 5000, "spent": 5200, "children": [
      { "categoryId": "...", "name": "Electricity", "isLeaf": true, "planned": 1500, "spent": 1700, "children": [] }
    ]}
  ]
}
```

## Risks

- Performance on large trees (mitigate with server aggregation + paging or virtual scroll)
- Leaf detection correctness if hierarchy changes concurrently
- Spent calculation depends on categorised transactions

## UI Decisions (Applied)

- Attribution control uses numeric inputs with a live total validator; the submit action is disabled unless total = 100
- No inline edits in the tree; allocation creation is performed via a dedicated dialog/form

## Definition of Done

- Specs, implementation, tests, and diagram updated
- All acceptance criteria in `specifications.md` met
- Basic a11y checks pass
- OpenAPI documented and verified in tests
