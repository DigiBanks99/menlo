# Budget Categories (Vertical) — Test Cases

## API

### Category CRUD (nested under budget)

- `POST /api/budgets/{budgetId}/categories` — create root category (happy path) → 201 with CategoryDto
- `POST /api/budgets/{budgetId}/categories` — create subcategory under root (happy path) → 201
- Create with duplicate name under same parent in same budget → 409 Conflict
- Create with parent soft-deleted → 400 ValidationProblemDetails
- Create with parent that is itself a child (depth > 2) → 400 ValidationProblemDetails
- Create with invalid budgetId → 404 NotFound
- Create with empty name → 400 ValidationProblemDetails
- `PUT /api/budgets/{budgetId}/categories/{categoryId}` — update name to duplicate sibling → 409 Conflict
- `PUT /api/budgets/{budgetId}/categories/{categoryId}` — update BudgetFlow, Attribution, IncomeContributor, ResponsiblePayer → 200
- `PUT /api/budgets/{budgetId}/categories/{categoryId}/reparent` — reparent to create depth > 2 → 400 ValidationProblemDetails
- `PUT /api/budgets/{budgetId}/categories/{categoryId}/reparent` — reparent to null (promote to root) → 200
- `DELETE /api/budgets/{budgetId}/categories/{categoryId}` — soft delete a category → 204; category excluded from default list
- `DELETE /api/budgets/{budgetId}/categories/{categoryId}` — soft delete a parent → cascades soft-delete to all children
- `PUT /api/budgets/{budgetId}/categories/{categoryId}/restore` — restore a soft-deleted category → 200; category visible again
- `PUT /api/budgets/{budgetId}/categories/{categoryId}/restore` — restore a parent → cascades restoration to all children
- Restore an already-active category → no-op, 200
- `GET /api/budgets/{budgetId}/categories/{categoryId}` — get by id not found → 404
- `GET /api/budgets/{budgetId}/categories/{categoryId}` — get by id from wrong budget → 404
- `GET /api/budgets/{budgetId}/categories` — list with `includeDeleted=false` (default) excludes deleted; `true` includes
- No year-based access constraints — any budget's categories can be managed

### CanonicalCategory implicit creation

- Creating a new category without an existing canonical mapping → auto-creates CanonicalCategory record
- Creating a category that matches an existing canonical entry → reuses existing CanonicalCategory
- CanonicalCategory `Name` and `Id` are set correctly on implicit creation
- CanonicalCategory audit fields (CreatedAt, CreatedBy) are populated on creation

### CloneForYear integration

- `Budget.CloneForYear()` clones the category tree from previous year's budget
- Cloned categories preserve `CanonicalCategoryId` for cross-year identity
- Cloned categories are independent (editing cloned category does not affect original)
- Cloning a budget with soft-deleted categories — only active categories are cloned

## Persistence

- Unique index `(BudgetId, ParentId, Name)` filtered on `IsDeleted = false` enforced
- Self-FK prevents cycles (depth limit enforced via domain logic)
- Soft-deleted categories remain in DB and can be restored
- `CanonicalCategories` table: Id (Guid PK), Name, audit fields populated correctly
- `CategoryNodes` table: all new columns (`CanonicalCategoryId`, `BudgetFlow`, `Attribution`, `IncomeContributor`, `ResponsiblePayer`, `IsDeleted`, `DeletedAt`, `DeletedBy`, audit fields) persisted correctly
- FK from `CategoryNodes.CanonicalCategoryId` → `CanonicalCategories.Id` enforced
- FK from `CategoryNodes.BudgetId` → `Budgets.Id` enforced
- Non-unique index on `(BudgetId, CanonicalCategoryId)` exists

## Read model

- Tree projection returns correct parent-child structure (2-level max)
- `includeDeleted=false` (default) excludes `IsDeleted=true` nodes
- `includeDeleted=true` includes all nodes
- Tree is scoped to a single budget (no cross-budget leakage)

## Domain validation

- BudgetFlow enum accepts only `Income`, `Expense`, `Both`
- Attribution enum accepts only `Main`, `Rental`, or null
- Depth validation: cannot create a child of a child (depth > 2)
- Name uniqueness: validated per budget, per parent, among active (non-deleted) siblings
- Cascade soft-delete: deleting a root soft-deletes all its children
- Cascade restore: restoring a root restores all its children
- PlannedMonthlyAmount returns `Money.Zero` (computed, not stored)

## Angular UI

- category-form validates required fields (name, budgetFlow) and prevents submit when invalid
- On 409, shows friendly duplicate name message and focuses name control
- On ProblemDetails.errors, maps field errors to controls
- category-tree toggles `includeDeleted` correctly
- category-tree displays expand/collapse for parent categories
- Inline add/edit/delete within budget detail view
- When adding a budget item (income), category pickers only show categories with BudgetFlow in (Income, Both); for expense, (Expense, Both)
- Client-side filtering for Attribution, IncomeContributor, ResponsiblePayer works correctly

## Observability & Security

- LoggerMessage entries emitted per endpoint path
- Traces include spans across UI → API → domain → EF save layers
- Auth required for all endpoints
- No rate limiting applied
