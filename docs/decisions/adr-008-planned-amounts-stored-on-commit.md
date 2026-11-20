# ADR-008: Planned Amounts Are Stored per Month, Written on Commit

## Status

**Accepted** - 2026-11-20

## Context

A budget line's planned amount for a month comes from layers: a baseline moved by dated changes and yearly changes, seasonal variations and conditional changes on top of it, and an override that can replace the result for one month. The plan changes only by commit, and a frozen month (any month before the current one) must keep the planned amounts it had while it was open, because its variance is judged against what was actually planned then.

Some later commits would produce different amounts for a frozen month if it were recalculated: deleting a seasonal variation, or moving a trigger whose date has passed. So the design has to say where a frozen month's planned amount comes from.

The decision was taken with the owner on [Decide how a budget line's plan is stored](https://github.com/DigiBanks99/menlo/issues/595), part of the [Budgeting and spend](https://github.com/DigiBanks99/menlo/issues/594) wayfinder map. The terms are in [`GLOSSARY.md`](../../GLOSSARY.md).

## Decision

- **The layers are the source of truth for open months.** Baselines, dated and yearly changes, seasonal variations, triggers with their conditional changes, event lines and overrides are stored as they are committed.
- **Planned amounts are stored per budget line per month.** Every commit recalculates the affected lines from the current month onwards and writes the results. Creating a budget by year copy writes all twelve months.
- **A month before the current month is never written.** Frozen months need no snapshot job and no versioned rules. Their stored amounts are the ones written by the last commit made while they were open.
- **Actuals are kept separately.** An actual is the sum of spends and can be recorded for any month. Variance is read as actual minus the stored planned amount.
- **Proposal previews are never stored.** A preview applies the switched-on proposals to the layers in memory and recalculates the open months for display.

## Options Considered

### 1. Stored per month, written on commit (Selected)

- **Pros**: Frozen months are structural, with nothing that runs when a month ends; variance, year-to-date and the views are plain reads; the data is small (about a hundred lines × twelve months a year)
- **Cons**: Two representations, the layers and the stored amounts, must be kept consistent by recalculating on every commit

### 2. Computed on read

- **Pros**: One representation; nothing to keep in step
- **Cons**: To reproduce a frozen month, every rule edit and trigger move must be versioned so that past months can be recalculated as the rules stood then. That is a lot of temporal bookkeeping just to protect frozen months.

### 3. Computed while open, snapshotted when a month freezes

- **Pros**: Stores only frozen months
- **Cons**: Needs something to run when a month ends, either a job or a lazy check on every read. It is easy to get wrong, and gains nothing over writing on commit, because the plan can only change at a commit.

## Consequences

- **A fix to the calculation doesn't reach frozen months.** A bug fix corrects the current and future months at the next commit. Frozen months keep the amounts they were planned with, which is intended.
- **There is no backdating.** A change missed in the past is committed from the current month, and the past months show it as variance. Correcting a past plan would be a separate Requirement.
- **There is no data migration.** The shipped budget-item model is replaced, and its production data is test data.
