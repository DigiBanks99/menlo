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

_Build succeeds. All 143 tests pass. Lint passes with 3 warnings._

### P1 - High (Core Missing Features per Specs)

#### Backend - Persistence Layer (Spec: persistence)

> **Status**: Core persistence infrastructure complete. DbContext, interceptors, converters, and entity configuration for User are implemented. Next: Add Budget entity configurations and create initial migration.

- [x] **Add EF Core NuGet packages** - Added to `Directory.Packages.props` and `Menlo.Api.csproj`: `Microsoft.EntityFrameworkCore` (10.0.2), `Microsoft.EntityFrameworkCore.Design` (10.0.2), `Npgsql.EntityFrameworkCore.PostgreSQL` (10.0.0), `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL` (13.1.0).
- [x] **Create Menlo.Persistence folder structure** - Created `src/api/Menlo.Api/Persistence/` with subfolders: `Data/`, `Configurations/`, `Interceptors/`, `Converters/`.
- [x] **Implement MenloDbContext** - Created `MenloDbContext : DbContext` with DbSet for User. Schema separation configured via entity configurations.
- [x] **Implement ISoftDeletable interface** - Created in domain at `Common/Abstractions/ISoftDeletable.cs` with: `IsDeleted`, `DeletedAt`, `DeletedBy`, `SoftDelete()`, `Restore()`.
- [x] **Implement ValueConverter for UserId** - Created `UserIdConverter` and `NullableUserIdConverter` in `Persistence/Converters/`.
- [x] **Implement ValueConverter for ExternalUserId** - Created `ExternalUserIdConverter` in `Persistence/Converters/`.
- [ ] **Implement ValueConverter for Money** - Map Money value object to amount + currency columns using `OwnsOne` pattern per spec (Section 9.1).
- [x] **Implement AuditingInterceptor** - Created `SaveChangesInterceptor` that calls `entity.Audit()` before SaveChanges.
- [x] **Implement SoftDeleteInterceptor** - Created with cascade soft delete to children via navigation properties.
- [x] **Create UserConfiguration** - Created `IEntityTypeConfiguration<User>` for `auth.users` table with audit columns and ID converters.
- [ ] **Create initial migration** - Run `dotnet ef migrations add InitialCreate` after Budget aggregate is implemented.
- [x] **Register DbContext in Program.cs** - Added `AddMenloPersistence()` extension method using hybrid approach (AddDbContext + EnrichNpgsqlDbContext).
- [ ] **Unit tests** - Add unit tests for all persistence features, including converters and interceptors

#### Backend - Budget Domain (Spec: budget-aggregate-minimum)

> **Blocker**: `/src/lib/Menlo.Lib/BudgetAggregateMinimum/` directory exists but is EMPTY. No Budget entity at all.

- [ ] **Create BudgetId strongly-typed ID** - `readonly record struct BudgetId(Guid Value)` in `Common/ValueObjects/`. Pattern exists for UserId.
- [ ] **Create BudgetCategoryId strongly-typed ID** - `readonly record struct BudgetCategoryId(Guid Value)`.
- [ ] **Create BudgetStatus enum** - `Draft`, `Active` values per spec (FR-1, FR-2).
- [ ] **Create Budget aggregate root** - Implement `IAggregateRoot<BudgetId>`, `IHasDomainEvents`, `IAuditable` with: Name, Year, Month, Currency, Status, Categories collection per spec.
- [ ] **Create BudgetCategory entity** - Child of Budget aggregate. Include: Name, Description, PlannedAmount (Money), ParentId (nullable for hierarchy), DisplayOrder per spec (FR-3).
- [ ] **Implement budget creation validation** - Name required, Year/Month valid, Currency ISO 4217, max depth 2 for categories per spec.
- [ ] **Implement category uniqueness rule** - Sibling categories must have unique names (case-insensitive) per spec (FR-6).
- [ ] **Implement total computation** - Sum children amounts to parent, sum all top-level to budget total per spec (FR-5).
- [ ] **Create BudgetError hierarchy** - Domain errors: `DuplicateBudgetError`, `InvalidCategoryError`, `CategoryHasChildrenError`, etc.
- [ ] **Create budget domain events** - `BudgetCreatedEvent`, `BudgetActivatedEvent`, `CategoryAddedEvent`, `PlannedAmountSetEvent` per spec.

#### Backend - Auditing (Spec: domain-auditing)

- [x] **Implement IAuditStampFactory** - Created `AuditStampFactory` in `Persistence/` that resolves current user from `HttpContext.User` claims (oid claim) and uses `TimeProvider.System.GetUtcNow()`.
- [x] **Register IAuditStampFactory in DI** - Registered as scoped in `AddMenloPersistence()` extension method.

#### Backend - Budget API Endpoints (Specs: budget-create-vertical, budget-categories-vertical)

> **Blocker**: Zero budget endpoints exist. Only auth endpoints are implemented.

- [ ] **Create POST /api/budgets endpoint** - Create budget command. Return 201 Created with Location header, 400 for validation errors, 409 for duplicate (user+period+name) per spec (FR-2).
- [ ] **Create GET /api/budgets endpoint** - List user's budgets with optional filtering by year/status.
- [ ] **Create GET /api/budgets/{id} endpoint** - Return budget DTO with categories tree and totals snapshot per spec (FR-5).
- [ ] **Create PUT /api/budgets/{id} endpoint** - Update budget name/description.
- [ ] **Create POST /api/budgets/{id}/activate endpoint** - Transition Draft to Active with validation per spec (FR-2).
- [ ] **Create category CRUD endpoints** - POST/PUT/DELETE for `/api/budgets/{id}/categories` per budget-categories-vertical spec.
- [ ] **Apply CanEditBudget policy** - Use `.RequireAuthorization(MenloPolicies.CanEditBudget)` on mutation endpoints.
- [ ] **Apply CanViewBudget policy** - Use `.RequireAuthorization(MenloPolicies.CanViewBudget)` on read endpoints.

#### Frontend - Budget UI (Specs: budget-create-vertical, budget-categories-vertical)

> **Current state**: UI components exist but use HARDCODED MOCK DATA. No real API integration.

- [ ] **Implement BudgetService** - Create service in `data-access-menlo-api` with: `getBudgets()`, `getBudget(id)`, `createBudget()`, `updateBudget()`, `deleteBudget()` using `HttpClient` and `toResult()` operator.
- [ ] **Implement CategoryService** - CRUD methods for categories using Result pattern.
- [ ] **Wire BudgetListComponent to BudgetService** - Replace hardcoded `budgets = signal([...])` with actual API calls via service.
- [ ] **Implement budget creation form** - Modal/dialog with reactive form: Name (required), Year (select), Month (select), Currency (select). Wire to `BudgetService.createBudget()`.
- [ ] **Implement budget details view** - Route `/budgets/:id` with category tree visualization and totals.
- [ ] **Wire BudgetAnalyticsComponent to real data** - Replace mock `totalBudget`, `spentThisMonth`, `categories` signals with API data.
- [ ] **Implement category management UI** - Add/edit/soft-delete categories with hierarchy visualization.
- [ ] **Implement form validation error mapping** - Use existing `mapValidationErrorsToForm()` helper with actual forms. Helper at `shared-util/src/lib/types/problem-details.ts:53`.

#### Frontend - Error Handling & UX (Spec: angular-result-pattern)

- [ ] **Implement toast/notification service** - Display non-validation errors from API calls. Result pattern infrastructure exists but no toast integration.
- [ ] **Create UnauthorizedComponent** - `roleGuard` redirects to `/unauthorized` but route/component doesn't exist.
- [ ] **Apply auth guards to budget routes** - Add `authGuard` and `roleGuard` to `/budgets` and `/analytics` routes in `app.routes.ts`. Guards exist but are not applied.

### P2 - Medium (Secondary Features)

#### AI Services (Spec: ai-infrastructure)

- [ ] **Complete VisionService implementation** - Currently throws `NotImplementedException("Vision service will be implemented in future phases")` at `src/lib/Menlo.AI/Services/VisionService.cs:10,15`. Wire to Phi-4-vision model.

#### Budget Allocation (Spec: budget-item)

- [ ] **Implement budget allocation endpoints** - POST to add planned amounts to leaf categories with attribution (Family/Rental/Shared percentages summing to 100%) per spec.
- [ ] **Implement allocation UI** - Form to set planned amounts on leaf categories with attribution validation.

#### Proactive Features (Spec: proactive-budget-adjustment)

- [ ] **Implement budget adjustment suggestions** - AI-driven reallocation suggestions with triggers, option sets, and approval workflow. Depends on: Budget aggregate, AI infrastructure.

#### Charts & Visualization

- [ ] **Integrate Chart.js** - `budget-analytics.component.html` has placeholder: "Chart visualization coming soon with Chart.js integration". Add chart library and implement category breakdown visualization.

### P3 - Low (Improvements & Tech Debt)

#### Lint Warnings (3 total)

- [ ] **Fix `@typescript-eslint/no-explicit-any` warnings**:
  - `src/ui/web/projects/menlo-lib/.storybook/main.ts:21:42`
  - `src/ui/web/projects/menlo-app/.storybook/main.ts:21:42`
  - `src/ui/web/projects/menlo-lib/src/lib/menlo-lib.spec.ts:11:6`

#### Test Configuration

- [ ] **Fix Angular test warnings** - `app.spec.ts` has warnings for missing `RouterOutlet` and `RouterLinkActive` imports in test setup.

#### Result Pattern Enhancement

- [ ] **Implement or remove curried form** - `result.spec.ts` has comment "Curried form not implemented - using standard two-argument form". Either implement or update spec to remove requirement.

---

## Specification-to-Implementation Gap Summary

| Specification               | Backend Status                                                                     | Frontend Status           | Priority |
| --------------------------- | ---------------------------------------------------------------------------------- | ------------------------- | -------- |
| domain-abstractions         | ✅ Complete                                                                         | N/A                       | Done     |
| domain-auditing             | ✅ Complete (AuditStampFactory)                                                     | N/A                       | Done     |
| money-domain                | ✅ Complete                                                                         | ✅ MoneyPipe exists        | Done     |
| user-id-resolution          | ✅ Complete                                                                         | N/A                       | Done     |
| authentication              | ✅ Complete (BFF pattern)                                                           | ✅ Complete (auth service) | Done     |
| persistence                 | ⚠️ 80% - DbContext, interceptors, User config done. Needs Budget config + migration | N/A                       | P1       |
| budget-aggregate-minimum    | ❌ Empty directory                                                                  | N/A                       | P1       |
| budget-create-vertical      | ❌ No endpoint                                                                      | ❌ Mock UI only            | P1       |
| budget-categories-vertical  | ❌ No endpoint                                                                      | ⚠️ Display only, mock data | P1       |
| budget-item                 | ❌ Not started                                                                      | ❌ Not started             | P2       |
| angular-result-pattern      | N/A                                                                                | ⚠️ No toast integration    | P1       |
| ui-layout                   | N/A                                                                                | ✅ Shell implemented       | Done     |
| ai-infrastructure           | ⚠️ VisionService placeholder                                                        | N/A                       | P2       |
| proactive-budget-adjustment | ❌ Not started                                                                      | ❌ Not started             | P2       |

---

## Implementation Order (Dependency-Aware)

```mermaid
graph TD
    A[1. Add EF Core packages] --> B[2. Create DbContext]
    B --> C[3. Create Budget aggregate]
    C --> D[4. Create entity configurations]
    D --> E[5. Create migrations]
    E --> F[6. Implement IAuditStampFactory]
    F --> G[7. Create budget API endpoints]
    G --> H[8. Create frontend services]
    H --> I[9. Wire UI to services]
```

---

## Validation Status

| Check          | Status | Details                                              |
| -------------- | ------ | ---------------------------------------------------- |
| Build          | ✅ PASS | 0 warnings, 0 errors                                 |
| Backend Tests  | ✅ PASS | 141 tests (Menlo.Lib.Tests: 92, Menlo.Api.Tests: 49) |
| AI Tests       | ✅ PASS | 2 tests                                              |
| Frontend Lint  | ✅ PASS | 0 errors, 3 warnings                                 |
| Frontend Tests | ✅ PASS | All projects pass                                    |

---

## In Progress

<!-- Move item here when starting work -->

## Completed

- [x] **Implement persistence layer foundation** - Created MenloDbContext, AuditingInterceptor, SoftDeleteInterceptor, UserConfiguration, ValueConverters (UserId, ExternalUserId), ISoftDeletable interface, and AuditStampFactory. Registered via `AddMenloPersistence()` extension method.
- [x] **Fix 18 failing API tests** - Fixed `TestWebApplicationFactory` to provide valid AzureAd configuration values and mock OpenIdConnect metadata. Tests now pass.
- [x] **Comprehensive codebase analysis** - Analyzed all specs vs implementation. Updated fix_plan.md with detailed gaps.
