# Budget Categories (Vertical) — Implementation Plan

## Architecture alignment and relationships

- **Budget-first approach**: categories are managed inline within a Budget aggregate. There is no standalone Category Catalog.
- Relation to Minimum Budget Aggregate (`budget-aggregate-minimum`):
  - The Budget aggregate owns its category tree (`CategoryNode`) with a depth limit of 2 levels.
  - Categories are **grouping containers only** — they do not hold planned amounts. `PlannedMonthlyAmount` is replaced with a computed display property returning `Money.Zero`. Budget items (a separate future vertical) will carry amounts.
  - `Budget.SetPlanned(categoryId, amount)` is **removed**.
  - `Budget.Activate()` validation (requires at least one non-zero planned amount) is **relaxed** until budget items provide real amounts.
- Relation to Create Budget (`budget-create-vertical`):
  - When a new Budget is created for year Y, its category tree is **automatically cloned from year Y-1's budget** via `Budget.CloneForYear()`. No user prompt, no catalog lookup, no fallback chain.
  - `CanonicalCategoryId` is preserved during cloning to maintain cross-year identity.
- **CanonicalCategory** bridge entity:
  - Provides stable cross-year identity for logical categories.
  - Lean entity: `Id` (Guid), `Name` (string), audit fields via `IAuditable`.
  - Implicitly created when a user adds a new category that doesn't map to an existing canonical entry.

## Contracts

- Domain types:

  - **`CanonicalCategory`** entity:
    - `Id` (Guid, PK)
    - `Name` (string)
    - `CreatedAt`, `CreatedBy`, `ModifiedAt`, `ModifiedBy` (via `IAuditable`)

  - **`CategoryNode`** entity (enriched, owned by Budget):
    - `Id` (Guid, PK)
    - `BudgetId` (Guid, FK → Budget)
    - `Name` (string)
    - `Description` (nullable string)
    - `ParentId` (nullable Guid, FK self)
    - `CanonicalCategoryId` (Guid, FK → CanonicalCategory) — cross-year identity link
    - `BudgetFlow` (enum: `Income`, `Expense`, `Both`) — controls which categories appear for income vs expense items
    - `Attribution` (nullable enum: `Main`, `Rental`) — whether the category is for the main household or rental apartment
    - `IncomeContributor` (nullable string, free text) — who earns income (for income categories)
    - `ResponsiblePayer` (nullable string, free text) — who pays (for expense categories)
    - Implements `ISoftDeletable` (`IsDeleted`, `DeletedAt`, `DeletedBy`)
    - Implements `IAuditable` (`CreatedAt`, `CreatedBy`, `ModifiedAt`, `ModifiedBy`)

  - **`BudgetFlow`** enum:
    ```csharp
    public enum BudgetFlow { Income, Expense, Both }
    ```

  - **`Attribution`** enum:
    ```csharp
    public enum Attribution { Main, Rental }
    ```

  - Invariants:
    - Name not empty; unique within the budget among siblings at the same level
    - ParentId must reference an existing, non-deleted root category within the same budget
    - Depth: 2 levels — a category's parent must be a root (parent's parent must be null)
    - BudgetFlow is required

- Commands/operations (on CategoryNode within Budget aggregate):
  - `CreateCategory(budgetId, name, budgetFlow, parentId?, description?, attribution?, incomeContributor?, responsiblePayer?) : Result<CategoryNode, Error>`
  - `UpdateCategory(budgetId, categoryId, changes) : Result<CategoryNode, Error>` (changes may include name, description, BudgetFlow, Attribution, IncomeContributor, ResponsiblePayer)
  - `ReparentCategory(budgetId, categoryId, newParentId | null) : Result<CategoryNode, Error>`
  - `SoftDeleteCategory(budgetId, categoryId) : Result<Unit, Error>` — cascades to children
  - `RestoreCategory(budgetId, categoryId) : Result<Unit, Error>` — cascades to children

- Events (optional for observability): CategoryCreated, CategoryUpdated, CategoryReparented, CategorySoftDeleted, CategoryRestored

## API

- Routes (nested under budget, kebab-case):
  - `POST /api/budgets/{budgetId}/categories` — create category
  - `GET /api/budgets/{budgetId}/categories` — list tree (supports `includeDeleted` query param, default false)
  - `GET /api/budgets/{budgetId}/categories/{categoryId}` — get by id
  - `PUT /api/budgets/{budgetId}/categories/{categoryId}` — update
  - `PUT /api/budgets/{budgetId}/categories/{categoryId}/reparent` — re-parent
  - `DELETE /api/budgets/{budgetId}/categories/{categoryId}` — soft delete (cascades to children)
  - `PUT /api/budgets/{budgetId}/categories/{categoryId}/restore` — restore (cascades to children)

- DTOs
  - Request: `CreateCategoryRequest { name: string; description?: string; parentId?: Guid; budgetFlow: 'income' | 'expense' | 'both'; attribution?: 'main' | 'rental'; incomeContributor?: string; responsiblePayer?: string }`
  - Update request: `UpdateCategoryRequest { name?: string; description?: string; budgetFlow?: 'income' | 'expense' | 'both'; attribution?: 'main' | 'rental'; incomeContributor?: string; responsiblePayer?: string }`
  - Reparent request: `ReparentCategoryRequest { newParentId?: Guid }` (null to promote to root)
  - Response: `CategoryDto { id: Guid; budgetId: Guid; name: string; description?: string; parentId?: Guid; canonicalCategoryId: Guid; budgetFlow: 'income' | 'expense' | 'both'; attribution?: 'main' | 'rental'; incomeContributor?: string; responsiblePayer?: string; isDeleted: boolean }`
  - Tree response:
    - `CategoryTreeNode { id: Guid; name: string; description?: string; budgetFlow: 'income' | 'expense' | 'both'; attribution?: 'main' | 'rental'; incomeContributor?: string; responsiblePayer?: string; isDeleted: boolean; children: CategoryTreeNode[] }`

- Errors
  - 400 ValidationProblemDetails (invalid name, depth violation, parent not root)
  - 404 NotFound (budgetId or categoryId not found)
  - 409 Conflict (uniqueness violation)

## Persistence (EF Core)

- Table `CanonicalCategories`
  - Columns: `Id` (Guid, PK), `Name`, `CreatedAt`, `CreatedBy`, `ModifiedAt`, `ModifiedBy`

- Table `CategoryNodes` (enriched)
  - Columns: `Id` (Guid, PK), `BudgetId` (FK → Budgets), `Name`, `Description`, `ParentId` (FK self), `CanonicalCategoryId` (FK → CanonicalCategories), `BudgetFlow`, `Attribution`, `IncomeContributor`, `ResponsiblePayer`, `IsDeleted`, `DeletedAt`, `DeletedBy`, `CreatedAt`, `CreatedBy`, `ModifiedAt`, `ModifiedBy`
  - Indexes:
    - Unique: `(BudgetId, ParentId, Name)` filtered on `IsDeleted = false` for selectable uniqueness
    - Non-unique: `(BudgetId, CanonicalCategoryId)` for cross-year lookups
  - FK: `FK_CategoryNodes_CategoryNodes_ParentId` with ON DELETE NO ACTION
  - FK: `FK_CategoryNodes_CanonicalCategories_CanonicalCategoryId`
  - FK: `FK_CategoryNodes_Budgets_BudgetId`

- Migration adds `CanonicalCategories` table and enriches `CategoryNodes` with new columns.

## Read model / projection

- Query builds a tree per budget:
  - Load all CategoryNodes for the budget (optionally include soft-deleted via `includeDeleted`)
  - Group by `ParentId` to construct `CategoryTreeNode[]`
  - Cache in-memory per request; consider server-side caching later.

## Observability & Security

- LoggerMessage source generators for each endpoint with clear names.
- Trace spans: UI submit → API endpoint → domain op → EF save → tree projection.
- Minimal metrics: counts of create/update/delete operations.
- Auth required for all endpoints; no rate limiting.

## Angular UI

- Follow Angular instructions (standalone components, signals, typed forms, Result pattern):
  - Category management is **inline in the budget detail view** (not a separate page or catalog view).
  - Components:
    - `category-tree` (tree with expand/collapse, inline add/edit/delete)
    - `category-form` (create/update) with controls: name, description, parentId, budgetFlow (income/expense/both), attribution (main/rental), incomeContributor, responsiblePayer
  - Service returns `Observable<Result<...>>` and maps ProblemDetails to form errors.
  - State with signals: `categories`, `selected`, `pending`, `result`.
  - UX rules:
    - Hide soft-deleted in category lists by default; show with toggle (`includeDeleted`).
    - Prevent selection of deleted categories for new budget items.
    - Client-side filtering for BudgetFlow, Attribution, payer, contributor.
  - When drafting items, enforce that income items only show categories with BudgetFlow in (Income, Both), and expense items only show (Expense, Both).

Integration in Create Budget flow (UI):

- After creating a Budget successfully, navigate to the budget detail view which loads the budget's own categories (cloned from previous year via `CloneForYear`).
- No catalog management page exists.

## Step-by-step tasks

1. Domain: add `CanonicalCategory` entity and `CategoryNode` enrichment (new fields, `ISoftDeletable`, `IAuditable`) + invariants (Result pattern) and tests.
2. Domain: implement cascade soft-delete/restore logic for parent→children.
3. Domain: implement implicit `CanonicalCategory` creation when adding a category without an existing canonical mapping.
4. API: Minimal API endpoints for budget-scoped category CRUD (`/api/budgets/{budgetId}/categories/...`) with validation, ProblemDetails.
5. Persistence: EF model for `CanonicalCategories` and enriched `CategoryNodes`, mappings, unique indexes (`BudgetId, ParentId, Name` filtered `IsDeleted=false`), migration, integration tests.
6. Read model: tree projection from budget's categories; list endpoint with `includeDeleted` filter; integration tests.
7. Observability: LoggerMessage source generators + minimal metrics; tracing.
8. Angular: services using Result operator; category-tree and category-form components with signals; form validation and error mapping; inline in budget detail view.
9. Integration with Create Budget: verify `Budget.CloneForYear()` preserves `CanonicalCategoryId` and clones the category tree; add integration tests.

## Definition of Done

- Docs (specs, implementation, tests, diagrams) updated and lint-clean.
- All endpoints implemented with tests (unit + integration).
- CanonicalCategory implicit creation tested.
- Cascade soft-delete/restore tested.
- UI components implemented with unit tests for validation and API error mapping.
- Observability and security checks in place.
- Create Budget uses `CloneForYear()` to initialise categories from the previous year's budget; tested.
- No references to Category Catalog remain in code or docs.
