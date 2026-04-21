# Budget Categories (Vertical) — Specifications

## Business Requirements

- Users can define, organise, and manage budget categories and subcategories **inline within a budget** to structure their finances.
- Categories are **owned by a Budget** — there is no standalone Category Catalog. Management happens in the budget detail view.
- Categories support hierarchical relationships (root → child) with a depth limit of **2 levels** (root → one level of children).
- Categories are **grouping containers only**; they do not hold planned amounts. Budget items (a separate future vertical) will carry amounts.
- Categories can be soft-deleted so they can no longer be used in new budget items, while preserving historical data. Soft-delete **cascades to children**; restore also cascades.
- A **CanonicalCategory** entity provides stable cross-year identity. When a category is added to a budget that doesn't map to an existing canonical entry, the system auto-creates the CanonicalCategory record. During `CloneForYear`, the `CanonicalCategoryId` is preserved.
- When creating a budget for year Y, the category tree is **automatically cloned from year Y-1's budget** via `Budget.CloneForYear()`. No user prompt, no catalog lookup.
- UX must provide clear feedback on errors (validation, conflicts) without exposing technical details.
- Categories carry optional metadata to speed up drafting:
  - `BudgetFlow` (Income, Expense, Both) — controls which categories appear for income vs expense items.
  - `Attribution` (nullable: Main, Rental) — whether the category is for the main household or rental apartment.
  - `IncomeContributor` (nullable, free text) — who earns income (for income categories).
  - `ResponsiblePayer` (nullable, free text) — who pays (for expense categories).

## Functional Requirements

### CategoryNode lifecycle

- Create category within a budget with required fields: `Name` (unique among siblings within the same budget), optional `Description`, optional `ParentId` (for subcategory), `BudgetFlow`, and optional `Attribution`, `IncomeContributor`, `ResponsiblePayer`.
- Create subcategory by assigning a parent category; parent must be a root (parent's parent must be null) and must not be soft-deleted.
- Update category details (name, description, BudgetFlow, Attribution, IncomeContributor, ResponsiblePayer) and re-parenting rules:
  - A category may be re-parented (or promoted to root) if it does not create cycles and respects the 2-level depth limit.
  - Renaming must maintain uniqueness among siblings within the same budget.
- Soft-delete a category with **cascade to children**:
  - Cannot be selected for new budget items.
  - Remain visible in historical views and reports.
  - Can be restored (un-delete), which also **cascades restoration to children**.
- Implements `ISoftDeletable` (IsDeleted, DeletedAt, DeletedBy) and `IAuditable` (CreatedAt, CreatedBy, ModifiedAt, ModifiedBy).

### CanonicalCategory

- Provides stable cross-year identity for logical categories across budget years.
- Fields: `Id` (Guid), `Name` (string), audit fields (CreatedAt, CreatedBy, ModifiedAt, ModifiedBy via `IAuditable`).
- Lean entity — no BudgetFlow, no defaults. Just identity + name + audit.
- **Implicitly created**: when a user adds a new category to a budget that doesn't map to an existing canonical entry, the system auto-creates the CanonicalCategory record.
- Each `CategoryNode` has a FK (`CanonicalCategoryId`) pointing to this entity.
- During `CloneForYear`, the `CanonicalCategoryId` is preserved so the same logical category is linked across years.

### Usage and constraints

- Category uniqueness: per budget, name must be unique among siblings at the same level.
- Depth policy: **2 levels** (root → child). A category's parent must be a root (parentId's parent must be null).
- Categories used in existing budget items cannot be hard-deleted.
- Scope: categories are scoped to a budget (no per-user ownership model).
- No restrictions on which year's budget can be edited — users can edit any year's categories.
- Audit: all lifecycle actions stamped with created/modified/deleted by/at.

### Query and drafting

- Read model provides category tree for a budget (root with children), with a single server-side filter:
  - `includeDeleted`: boolean (default false)
- All other filtering (search, BudgetFlow, Attribution, payer, contributor) is done **client-side in Angular**.
- The category tree per budget is small enough (~200 nodes max) for client-side filtering to be performant.
- API supports listing the category tree for a budget, fetching a single category by ID.

### Security and access

- Auth required for all endpoints.
- No rate limiting — this is a family app, not a public API.
- No year-based access constraints — users can edit any year's categories.

## Non-functional Requirements

- Observability: structured logging (LoggerMessage source generators), minimal metrics (counts of create/update/delete), tracing across UI→API→domain→DB.
- Performance: list tree should render in <150ms from API for typical sizes (≤ 200 categories per budget).
- Reliability: category operations are idempotent where applicable (e.g., restore already-active = no-op).

## Dependencies

- Reuses Domain Abstractions (strongly-typed IDs using Guid, Result pattern, IAuditable, ISoftDeletable).
- Integrates with Budget aggregate (`Budget.CloneForYear()` for year-over-year category cloning).
- Frontend follows Angular instructions (standalone components, signals, typed forms, Result pattern for API).

## Deferred

- Category Catalog (standalone template registry) — entirely deferred.
- Save-as-template functionality.
- Cross-year analysis endpoints.
- Color and icon fields on CategoryNode.
- Feature toggles.
- Budget item entity (separate vertical).

## Diagrams

- See diagrams folder for vertical flow and data model.
