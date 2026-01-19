# Fix Plan

> This file is maintained by Ralph. Run `ralph.sh plan` to regenerate from scratch.

## How This File Works

- **Priority Items**: Work to be done, sorted by importance
- **In Progress**: Currently being worked on (should be 0-1 items)
- **Completed**: Recently finished items (clean up periodically)

## Maintenance Rules

1. When this file exceeds 100 items, trigger cleanup:
   - Remove all completed items
   - Consolidate duplicate/related items
   - Re-prioritize remaining work

2. When encountering bugs during implementation:
   - Add to Priority Items FIRST
   - Then spawn subagent to fix

3. Format for items:
   - `- [ ]` Pending
   - `- [x]` Completed

---

## Priority Items

### P0 - Critical (Blocking Validation)

_No P0 items at this time._

### P1 - High (Core Missing Features per Specs)

#### Backend - Persistence Layer (Spec: persistence)
- [ ] **Implement PostgreSQL DbContext** - Create `MenloDbContext` with EF Core configuration for PostgreSQL using Npgsql provider. No DbContext exists currently.
- [ ] **Implement entity configurations** - Create `IEntityTypeConfiguration<T>` for User, Budget, Category entities with proper table mappings, indexes, and constraints.
- [ ] **Implement database migrations** - Create initial migration with `dotnet ef migrations add`. Configure auto-migration on startup via Hosted Service per spec.
- [ ] **Implement repository pattern** - Create `IRepository<T>` abstraction and EF Core implementation for data access.

#### Backend - Budget Domain (Spec: budget-aggregate-minimum)
- [ ] **Implement Budget aggregate root** - Create `Budget` entity in `/src/lib/Menlo.Lib/BudgetAggregateMinimum/` (directory exists but is empty). Include: Name, Year, Month, Currency, Status (Draft/Active), categories collection.
- [ ] **Implement Category entity** - Hierarchical categories with max depth 2. Include: Name (unique per budget, case-insensitive), PlannedAmount (Money), parent reference.
- [ ] **Implement budget total computation** - Auto-calculate totals at category, parent, and overall budget levels per spec.
- [ ] **Implement budget validation** - Category uniqueness, planned amounts >= 0, currency precision rules.
- [ ] **Implement budget duplication** - Clone budget with categories for new period.

#### Backend - Budget Verticals (Specs: budget-create-vertical, budget-categories-vertical, budget-item)
- [ ] **Implement IAuditStampFactory** - Interface exists but no implementation. Required by domain auditing spec. Should resolve current user and UTC timestamp.
- [ ] **Implement POST /api/budgets endpoint** - Create budget command handler with validation, persistence, and 201/400/409 responses per spec.
- [ ] **Implement budget category CRUD endpoints** - GET/POST/PUT/DELETE for categories with soft-delete and restore support per spec.
- [ ] **Implement budget item endpoints** - Add/update planned allocations for leaf categories per spec.

#### Frontend - Budget UI (Specs: budget-create-vertical, budget-categories-vertical)
- [ ] **Implement budget creation form** - Modal/dialog with Name, Year, Month, Currency inputs. Wire to POST /api/budgets.
- [ ] **Implement category management UI** - Add/edit/delete categories with hierarchy visualization. Currently display-only with mock data.
- [ ] **Wire budget list to real API** - Replace mock data in `BudgetListComponent` with actual API calls via `MenloApiClient`.
- [ ] **Implement budget item allocation UI** - Form to add planned amounts to leaf categories per spec.

#### Frontend - Error Handling (Spec: angular-result-pattern)
- [ ] **Implement toast/snackbar service** - Display non-validation errors as toasts per spec. Result pattern infrastructure exists but no toast integration.
- [ ] **Implement form validation error mapping** - Use `mapValidationErrorsToForm()` helper with actual forms. Helper exists but no integration.

#### Frontend - Route Protection
- [ ] **Apply auth guards to routes** - `authGuard` and `roleGuard` are implemented but not applied to `/budgets` or `/analytics` routes.

### P2 - Medium (Secondary Features)

#### AI Services (Spec: ai-infrastructure)
- [ ] **Implement VisionService** - Currently throws `NotImplementedException`. Integrate Phi-4-vision model per spec. Deferred to future phase per code comment.

#### Proactive Features (Spec: proactive-budget-adjustment)
- [ ] **Implement budget adjustment suggestions** - AI-driven reallocation suggestions with triggers, option sets, and approval workflow. Depends on: Budget aggregate, AI infrastructure.

### P3 - Low (Improvements & Tech Debt)

#### Lint Warnings
- [ ] **Fix `@typescript-eslint/no-explicit-any` warnings** - 3 occurrences in Storybook config files (`menlo-lib/.storybook/main.ts:21`, `menlo-app/.storybook/main.ts:21`) and test file (`menlo-lib/src/lib/menlo-lib.spec.ts:11`).

#### Test Configuration
- [ ] **Fix Angular test warnings** - `app.spec.ts` has warnings for missing `RouterOutlet` and `RouterLinkActive` imports in test setup.

#### Result Pattern
- [ ] **Implement or remove curried form** - `result.spec.ts` has skipped test `TC-022: should support curried form`. Either implement or update spec to remove requirement.

---

## Specification-to-Implementation Gap Summary

| Specification | Backend Status | Frontend Status | Priority |
|--------------|----------------|-----------------|----------|
| domain-abstractions | ✅ Complete | N/A | Done |
| domain-auditing | ⚠️ Missing IAuditStampFactory | N/A | P1 |
| money-domain | ✅ Complete | ✅ MoneyPipe exists | Done |
| user-id-resolution | ✅ Complete | N/A | Done |
| authentication | ✅ Complete | ✅ Complete | Done |
| persistence | ❌ Not started | N/A | P1 |
| budget-aggregate-minimum | ❌ Empty directory | N/A | P1 |
| budget-create-vertical | ❌ No endpoint | ❌ Mock UI only | P1 |
| budget-categories-vertical | ❌ No endpoint | ⚠️ Display only | P1 |
| budget-item | ❌ Not started | ❌ Not started | P1 |
| angular-result-pattern | N/A | ⚠️ No toast integration | P1 |
| ui-layout | N/A | ✅ Shell implemented | Done |
| ai-infrastructure | ⚠️ VisionService placeholder | N/A | P2 |
| proactive-budget-adjustment | ❌ Not started | ❌ Not started | P2 |

---

## In Progress

<!-- Move item here when starting work -->

## Completed

- [x] **Fix 18 failing API tests** - Fixed `TestWebApplicationFactory` to provide valid AzureAd configuration values and mock OpenIdConnect metadata. Tests now pass.
