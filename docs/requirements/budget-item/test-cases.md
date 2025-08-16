# Test Cases: Add Budget Item from UI

Link: `specifications.md`

## Legend

- BDD style where helpful; minimal and testable; trace to FRs in specs

## TC-01 Create Allocation (Happy Path)

- Pre: Budget year exists; leaf category exists; month within year
- Steps:
  1. Open Budget UI, select month
  2. Click Add Allocation, choose leaf category, enter amount R 1,250.00, attribution: Family 100%
  3. Submit
- Expected:
  - 201 Created with allocation ID (FR-1, FR-2)
  - Domain event emitted: BudgetAllocationPlanned (FR-7)
  - UI refresh shows item in tree (FR-3, FR-4)

## TC-02 Validation: Non-leaf Category Rejected

- Steps: Attempt to add allocation to a parent category
- Expected: 400 BadRequest with message "Allocations can only be created on leaf categories" (FR-5)

## TC-03 Validation: Attribution Sum ≠ 100

- Steps: Family 70%, Rental 20%
- Expected: 400 with message "Attribution must total 100%" (FR-5)

## TC-04 Validation: Amount ≤ 0

- Steps: Enter R 0.00 or negative number
- Expected: 400 with message "Amount must be greater than zero" (FR-5)

## TC-05 Validation: Month Outside Budget Year

- Steps: Select month not in budget year
- Expected: 400 with message "Month is outside of budget period" (FR-5)

## TC-06 Persistence and Read-back

- Steps: Create allocation; refresh page
- Expected: Item is returned by GET tree endpoint and displayed (FR-2, FR-3, FR-4)

## TC-07 Aggregation Correctness

- Pre: Two child leaf allocations under a parent
- Steps: Create R 500 and R 800 under same parent
- Expected: Parent planned total = R 1,300; roll-up correct (FR-3, FR-4)

## TC-08 Visualisation: Progress Bars & Over-Budget

- Pre: Planned R 1,000; Spent R 1,200 tagged to same category
- Expected: Progress shows >100% with over-budget indicator (FR-4)

## TC-09 Filters

- Steps: Toggle Over budget filter
- Expected: Only nodes with Spent > Planned remain visible (FR-4)

## TC-10 Accessibility

- Steps: Navigate tree via keyboard (Tab, Arrow keys), screen reader labels
- Expected: Focus order logical, ARIA roles set, text alternatives present (FR-6)

## TC-11 Telemetry Hygiene

- Steps: Create allocation
- Expected: Telemetry event recorded without PII; includes feature toggle state (FR-7)
