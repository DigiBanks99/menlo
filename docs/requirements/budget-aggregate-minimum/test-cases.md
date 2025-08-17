# Test Cases: Minimum Budget Aggregate Surface

## Scope

Covers domain-level behaviour for the minimal Budget aggregate: categories (depth ≤ 2), planned amounts, computed totals, and activation gating. No persistence or API.

## Test Matrix

- Creation
  - Given valid name/period/currency When creating a Budget Then it’s Draft with empty categories
  - Given invalid month (0 or 13) When creating a Budget Then it fails with validation error
- Categories
  - Given a draft budget When adding a top-level category Then it succeeds and is listed
  - Given a parent category When adding a subcategory Then depth is 2 and it succeeds
  - Given a subcategory When adding a sub-subcategory Then it fails with MaxDepthExceeded
  - Given sibling names differing only by case When adding Then it fails with DuplicateCategoryName
  - Given a category with children When removing Then it fails with CategoryHasChildren
  - Given a category with planned > 0 When removing Then it fails with CategoryNotLeaf
  - Given categories with orders When reordering Then display order changes but totals do not
- Planned amounts
  - Given a category When setting planned with negative amount Then it fails with InvalidAmount
  - Given a currency with 2 fraction digits When setting planned with 3 decimals Then it fails with InvalidAmount
  - Given a category When clearing planned Then amount is null and totals update
- Totals
  - Given a parent with planned and children with planned When computing totals Then per-node totals equal own+children and overall is sum of top-level
  - Given only children planned When computing totals Then parent totals reflect children
  - Given no planned amounts anywhere When computing totals Then overall is 0
- Activation
  - Given a draft budget with all invariants valid but all planned = 0 When activating Then it fails with ActivationValidationFailed
  - Given a draft budget with at least one non-zero planned and valid invariants When activating Then Status becomes Active

## Non-functional

- Deterministic totals irrespective of category order
- Auditing invoked on mutations with IAuditStampFactory

## Traceability

- Maps to requirements in `specifications.md` and design in `implementation.md`.
