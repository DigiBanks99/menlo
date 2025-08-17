# Budget Categories (Vertical) — Implementation Plan

## Architecture alignment and relationships

- Relation to Minimum Budget Aggregate (`budget-aggregate-minimum`):
  - The Budget aggregate owns its own category tree (`CategoryNode`) with planned amounts and a depth limit of 2.
  - This vertical manages a Category Catalog (templates) per budget year that is used to initialise a new Budget’s category tree.
  - Depth and naming invariants are identical to ensure smooth cloning.
- Relation to Create Budget (`budget-create-vertical`):
  - When a new Budget is created, its category tree is cloned from the Category Catalog for the specified year.
  - Later changes to the catalog do not retroactively change existing budgets (no auto-sync). A future manual sync/migration tool can be added (deferred).

## Contracts

- Domain types:
  - Category Catalog template entity: `CategoryTemplate` with fields:
    - TemplateId, BudgetYear (e.g., 2025), Name, Description?, Color?, Icon?, ParentTemplateId?, DirectionPolicy, DefaultAttribution?, DefaultIncomeContributor?, DefaultResponsiblePayer?, IsDeleted, AuditStamp
  - Invariants:
    - Name not empty; unique within the budget year among siblings at the same level
    - ParentTemplateId must reference an existing, non-deleted root template
    - Depth: ParentTemplateId can only reference root (one-level hierarchy)
    - DirectionPolicy is one of: IncomeOnly | ExpenseOnly | Both
- Commands/operations:
  - `CreateCategoryTemplate(name, parentTemplateId?, directionPolicy, defaults?) : Result<CategoryTemplate, Error>`
  - `UpdateCategoryTemplate(templateId, changes) : Result<CategoryTemplate, Error>` (changes may include directionPolicy and defaults)
  - `ReparentCategoryTemplate(templateId, newParentTemplateId | null) : Result<CategoryTemplate, Error>`
  - `SoftDeleteCategoryTemplate(templateId) : Result<Unit, Error>` and `RestoreCategoryTemplate(templateId) : Result<Unit, Error>`
- Events (optional for observability): CategoryCreated, CategoryUpdated, CategoryReparented, CategorySoftDeleted, CategoryRestored

Concept mapping:

- Catalog `CategoryTemplate` → cloned to Budget aggregate `CategoryNode` during budget creation.
  - Planned amounts are not part of templates and remain null/default in the clone.

## API

- Routes (kebab-case):
  - `POST /api/category-catalog` — create template
  - `GET /api/category-catalog` — list (supports `budgetYear`, `includeDeleted`, `search`, `onlySelectable`, `direction`, `attribution`, `payer`, `contributor`)
  - `GET /api/category-catalog/{templateId}` — get by id
  - `PUT /api/category-catalog/{templateId}` — update
  - `PUT /api/category-catalog/{templateId}/reparent` — re-parent
  - `DELETE /api/category-catalog/{templateId}` — soft delete
  - `PUT /api/category-catalog/{templateId}/restore` — restore
  - Integration endpoint (optional, internal): `POST /internal/budgets/{budgetId}/categories:clone-from-year/{year}` (used by Create Budget handler instead of being public)
- DTOs
  - Request: `CreateCategoryTemplateRequest { budgetYear: number; name: string; description?: string; color?: string; icon?: string; parentTemplateId?: Ulid; directionPolicy: 'income' | 'expense' | 'both'; defaults?: { attribution?: 'main' | 'rental'; incomeContributor?: string; responsiblePayer?: string } }`
  - Response: `CategoryTemplateDto { templateId: Ulid; budgetYear: number; name: string; description?: string; color?: string; icon?: string; parentTemplateId?: Ulid; directionPolicy: 'income' | 'expense' | 'both'; defaults?: { attribution?: 'main' | 'rental'; incomeContributor?: string; responsiblePayer?: string }; isDeleted: boolean }`
  - List response returns a flat list and a tree shape:
    - `CategoryTemplateNode { templateId, budgetYear, name, isDeleted, directionPolicy, defaults?, children: CategoryTemplateNode[] }`
- Errors
  - 400 ValidationProblemDetails (invalid name, depth, parent)
  - 404 NotFound (templateId/parentTemplateId not found or not in the specified budget year)
  - 409 Conflict (uniqueness violation)

## Persistence (EF Core)

- Table `CategoryCatalog`
  - Columns: TemplateID (PK), BudgetYear (IX), Name, Description, Color, Icon, ParentTemplateID (FK self), DirectionPolicy, DefaultAttribution, DefaultIncomeContributor, DefaultResponsiblePayer, IsDeleted, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, DeletedAt, DeletedBy
  - Indexes:
    - Unique: (BudgetYear, ParentTemplateID, Name) filtered on IsDeleted = false for selectable uniqueness
    - Non-unique search index on (BudgetYear, Name)
  - FK: `FK_CategoryCatalog_CategoryCatalog_ParentTemplateID` with ON DELETE NO ACTION
- Migrations for new table; seed none.

## Read model / projection

- Query builds a tree per budget year:
  - Load all category templates for the budget year (optionally filter by includeDeleted)
  - Group by ParentTemplateID to construct `CategoryTemplateNode[]`
  - Cache in-memory per request; consider server-side caching later.

## Observability & Security

- LoggerMessage source generators for each endpoint with clear names.
- Trace spans: UI submit → API endpoint → domain op → EF save → projection.
- Policies: constrain requests to the active budget year; soft-delete protected by policy.

## Angular UI

- Follow Angular instructions (standalone components, signals, typed forms, Result pattern):
  - Components:
  - `category-catalog-list` (tree with expand/collapse, search, includeDeleted toggle)
  - `category-catalog-form` (create/update) with controls: budgetYear (default current), name, description, color, icon, parentTemplateId, directionPolicy (income/expense/both), defaults (attribution, incomeContributor, responsiblePayer)
  - Service returns `Observable<Result<...>>` and maps ProblemDetails to form errors.
  - State with signals: `categories`, `selected`, `pending`, `result`.
  - UX rules:
    - Hide soft-deleted in selectable lists by default; show with toggle.
    - Prevent selection of deleted categories for new budget items.
  - When drafting items, enforce that income items only show categories with directionPolicy in (income, both), and expense items only show (expense, both).

Integration in Create Budget flow (UI):

- After creating a Budget successfully, navigate to the budget view which loads the budget’s own categories (cloned) rather than the catalog.

## Step-by-step tasks

1. Domain: add Category model + operations + invariants (Result pattern) and tests.
1. API: Minimal API endpoints for Category Catalog with validation, ProblemDetails; constrain by budget year.
1. Persistence: EF model for CategoryCatalog, mapping, unique indexes (BudgetYear, ParentTemplateID, Name, filtered IsDeleted=false), migration, integration test for uniqueness and cascading rules.
1. Read model: tree projection; list endpoints (filters including direction/attribution/payer/contributor); integration tests.
1. Observability: LoggerMessage + minimal metrics; tracing.
1. Angular: services using Result operator; components with signals; form validation and error mapping.
1. Feature toggle: allow enabling/disabling category management per environment.
1. Integration with Create Budget: implement `CategoryCatalogCloner` used by Create Budget handler to clone templates into Budget aggregate categories; add integration tests in the Create Budget vertical.

## Definition of Done

- Docs (specs, implementation, tests, diagrams) updated and lint-clean.
- All endpoints implemented with tests (unit + integration).
- UI components implemented with unit tests for validation and API error mapping.
- Observability and security checks in place.
- Create Budget uses the cloner to initialise categories from the catalog for the selected budget year; tested.
