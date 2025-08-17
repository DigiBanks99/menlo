# Budget Categories (Vertical) — Specifications

## Business Requirements

- Users can define, organise, and manage budget categories and subcategories to structure their budgets.
- Categories support hierarchical relationships (category → subcategories) with practical limits to prevent deep nesting complexity (1 level of subcategories to start).
- Categories can be soft-deleted so they can no longer be used in new budget items, while preserving historical data.
- Category management does not rely on per-user/household ownership. The system will typically have a single budget per year; categories are scoped to that budget year.
- The system must make it easy to maintain categories during budget drafting and later modification.
- UX must provide clear feedback on errors (validation, conflicts) without exposing technical details.
- Categories may optionally carry defaults to speed up drafting:
  - Attribution: main household or rental apartment
  - Contributor to income (for income categories)
  - Responsible payer (for expense categories)

## Functional Requirements

### Category lifecycle

- Create category with required fields: name (unique among siblings within the same budget year), optional description, optional colour/icon, and optional parent (for subcategory).
- Create subcategory by assigning a parent category; parent must not be soft-deleted.
- Update category details (name, description, colour/icon) and re-parenting rules:
  - A category may be re-parented (or promoted to root) if it does not create cycles and respects depth limit.
  - Renaming must maintain uniqueness among siblings within the same budget year.
- Soft delete a category (and optionally cascade-disable all subcategories). Soft-deleted categories:
  - Cannot be selected for new budget items or templates.
  - Remain visible in historical views and reports.
  - Can be restored (un-delete) if needed.

### Usage and constraints

- Category uniqueness: per budget year, name must be unique among siblings at the same level.
- Depth policy: one subcategory level (root → child) for MVP.
- Categories used in existing budget items cannot be hard-deleted.
- Scope: categories are scoped to a budget year (no per-user ownership model).
- Audit: all lifecycle actions stamped with created/updated/deleted by/at.

### Query and drafting

- Read model provides category tree for drafting (root with children), with filters:
  - includeDeleted: boolean (default false)
  - search: string
  - onlySelectable: boolean (exclude soft-deleted)
  - attribution: enum filter (main, rental, all)
  - payer: optional filter (responsible payer)
  - contributor: optional filter (income contributor)
- API supports listing categories, fetching a single category, and searching by name.

### Security and access

- Auth required for all endpoints. No per-owner tenancy; if/when multiple years are present, ensure requests are constrained to the active budget year.
- Rate limit or throttle category mutations to prevent accidental rapid changes.

## Non-functional Requirements

- Observability: structured logging (LoggerMessage), minimal metrics (counts of create/update/delete), tracing across UI→API→DB.
- Performance: list tree should render in <150ms from API for typical sizes (<= 200 categories per owner).
- Reliability: category operations are idempotent where applicable (e.g., restore already-active = no-op).

## Dependencies

- Reuses Domain Abstractions (strongly-typed IDs, Result pattern, AuditStamp).
- Integrates with Minimum Budget Aggregate for item categorisation.
- Frontend follows Angular instructions (signals, typed forms, Result pattern for API).

## Diagrams

- See diagrams folder for vertical flow and data model.
