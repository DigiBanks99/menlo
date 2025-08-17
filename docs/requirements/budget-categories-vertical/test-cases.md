# Budget Categories (Vertical) — Test Cases

## API

- Create root category (happy path) → 201 with CategoryDto
- Create subcategory under root (happy path) → 201
- Create with duplicate name under same parent → 409 Conflict
- Create with parent soft-deleted → 400 ValidationProblemDetails
- Update name to duplicate sibling → 409 Conflict
- Reparent to create depth > 1 → 400 ValidationProblemDetails
- Soft delete a category → 204; category excluded from selectable lists
- Restore a soft-deleted category → 204; category selectable again
- Get by id not found/other budget year → 404
- List with includeDeleted=false (default) excludes deleted; true includes
- List filtered by direction=income excludes expense-only categories, and vice versa
- List filtered by attribution=rental shows only categories with rental attribution defaults (or none if filtering strictly)

## Persistence

- Unique index (BudgetYear, ParentID, Name, IsDeleted=false) enforced
- Self-FK prevents cycles (depth limit enforced via domain logic)
- Soft-deleted categories remain in DB and can be restored

## Read model

- Tree projection returns correct parent-child structure
- Search filter returns name contains (case-insensitive)
- onlySelectable excludes IsDeleted=true
- direction filter respects DirectionPolicy
- attribution/payer/contributor filters respected

## Angular UI

- category-form validates required fields and prevents submit when invalid
- On 409, shows friendly duplicate name message and focuses name control
- On ProblemDetails.errors, maps field errors to controls
- category-list toggles includeDeleted and search filters correctly
- When adding a budget item (income), category pickers only show income/both; for expense, expense/both

## Observability & Security

- LoggerMessage entries emitted per endpoint path
- Traces include spans across layers
- Requests constrained to active budget year
