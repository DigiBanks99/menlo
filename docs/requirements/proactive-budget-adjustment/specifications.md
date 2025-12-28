# Proactive Budget Adjustments — Specifications

## Overview

Objective: Provide explainable, non-intrusive suggestions to reallocate budget across categories to keep the plan realistic and balanced while respecting locks, minimums, attribution, and tax flags.

Out of scope (for this requirement):

- Automatic application of changes without explicit user approval
- Real-time co-authoring with AI (separate requirement)

## Business Requirements

- BR-1 Explainable Suggestions
  - The system must provide clear, explainable suggestions for budget reallocation, referencing triggers and rationale.

- BR-2 Net-Zero Reallocation
  - By default, suggestions must net to zero across categories unless the user explicitly changes the total allocation for the month.

- BR-3 Respect Constraints
  - Locked categories, minimums, earmarked funds, and attribution/tax flags must be respected in all suggestions.

- BR-4 User Approval
  - No suggestion is auto-applied; explicit user approval is required for any change.

- BR-5 Persona Targeting
  - Suggestions and notifications must be delivered according to user persona preferences (CFO/COO) and user delivery settings.

- BR-6 Auditability and Reversibility
  - All accepted suggestions must be auditable and reversible with a one-click undo, preserving prior state.

## Functional Requirements

- FR-1 Generate Suggestions
  - Generate reallocation suggestions when defined triggers are met (threshold crossing, list impact, trend deviation, event change, underutilisation).

- FR-2 Option Sets with Confidence and Rationale
  - Each suggestion must include 1–3 option sets (Conservative, Balanced, Aggressive) with confidence, reason code, rationale, and affected categories.

- FR-3 Enforcement of Constraints
  - Enforce locked category immutability and honour category minimums and earmarked funds; preserve attribution and tax-deductible flags.

- FR-4 Accept and Undo
  - Upon acceptance, apply changes and create auditable ledger entries; provide immediate undo that restores the prior allocation exactly.

- FR-5 Notifications and Inbox
  - Notify users of new suggestions and surface them in a “Budget Suggestions” inbox with filters (month, category, trigger, confidence); support quiet hours and digest modes.

- FR-6 Inline Modification
  - Allow inline edits to suggestion amounts and swapping of source/target categories prior to acceptance.

- FR-7 Learning from Outcomes
  - Learn from Accept/Modify/Dismiss outcomes to tune future suggestions and option sets; operate locally without sending PII externally.

## Triggers

- Threshold crossing: Category/envelope hits configurable thresholds (e.g., 80%, 95% of month allocation).
- List impact: Completed or updated planning lists add expected costs to mapped categories.
- Trend deviation: Overspend vs historical/seasonal patterns for the same period in month.
- Income/event change: Income variance, one-off events, or calendar-linked expenses due soon.
- Underutilisation: Surplus detected in low-usage categories to fund pressure areas.

## Rules and Constraints

- Suggestions are never auto-applied; explicit user approval is required.
- Net-zero by default: Moves must net to zero unless user explicitly changes total monthly allocation.
- Respect protected constraints:
  - Locked categories are immutable.
  - Category minimums and earmarked funds cannot be breached.
  - Personal vs rental attribution and tax-deductible flags must be preserved or explicitly confirmed.
- Provide 1–3 option sets per suggestion: Conservative, Balanced, Aggressive.
- Each suggestion includes: confidence, reason code (THRESHOLD_REACHED, LIST_IMPACT, TREND_DEVIATION, EVENT_UPCOMING, UNDERUTILISED), rationale, and affected categories.
- Accepted suggestions create auditable ledger entries; all are reversible with “Undo”.

## Glossary

- Net-zero: A suggestion where the sum of increases equals the sum of decreases across categories for the month.
- Reason code: Canonical driver for a suggestion (THRESHOLD_REACHED, LIST_IMPACT, TREND_DEVIATION, EVENT_UPCOMING, UNDERUTILISED).
- Confidence: A numeric or banded indicator of suggestion reliability based on observed signals.
- Locked category: Category that cannot be reduced or reallocated from.

## User Experience

- “Budget Suggestions” inbox with filters (month, category, trigger, confidence).
- One-click actions: Accept, Modify, Dismiss, Snooze, Bulk Accept.
- Inline edits before acceptance (adjust amounts, swap source/target categories).
- Immediate undo and full audit trail.
- Persona targeting:
  - CFO (Husband): gets all reallocation suggestions (default).
  - COO (Wife): gets list-derived suggestions and summaries (opt-in).

## Notifications

- Reallocation suggestion available: summary with top affected categories and net-zero flag.
- Quiet hours and daily digest modes.
- Per-user delivery preferences and per-trigger opt-out.
- Ties into the global Notification System defined in business requirements.

## Explainability and Learning

- “Why this suggestion?” panel shows primary trigger, thresholds crossed, trend deltas, and recent list/transaction drivers.
- Learns from Accept/Modify/Dismiss outcomes to tune sensitivity and option sets.
- All learning and inference are processed locally (Ollama + Semantic Kernel).

## Acceptance Criteria

- [ ] AC-1 A suggestion is generated within 5 minutes when a category crosses a configured threshold, offering at least one net-zero option.
- [ ] AC-2 Locked categories and defined minimums are never reduced by system-proposed options.
- [ ] AC-3 Accepted suggestions record auditable entries: before/after amounts, user, timestamp, reason code, and attribution/tax flags.
- [ ] AC-4 Undo restores the prior allocation exactly without side effects.
- [ ] AC-5 Suggestions that involve rental/personal splits preserve attribution and clearly show tax-deductible impact.
- [ ] AC-6 Each suggestion includes a concise explanation referencing the trigger and contributing data points.

## Non-Functional Requirements

- Latency: Suggestion generation for threshold/list triggers within 5 minutes; suggestions inbox loads in under 3 seconds on typical home network.
- Privacy: 100% local processing; no PII leaves the home server.
- Availability: Graceful degradation if AI offline; allow manual reallocation and show last known state.
- Observability: Minimal local telemetry (counts by trigger/outcome, latencies) without PII; optional export.
- Configurability: Per-category thresholds, lock/minimum flags, trigger sensitivity, and persona delivery.

## Configuration

- Global settings: enable/disable feature; quiet hours; digest schedule; default thresholds.
- Per-category settings: thresholds, sensitivity, seasonal flag, locked/minimum/earmarked constraints.
- Persona delivery: per-user delivery preferences and per-trigger opt-outs.

## Assumptions and Dependencies

- Single-family scope (non-multi-tenant) per programme constraints.
- Local AI stack available (Ollama + Semantic Kernel) for ranking and rationale; rules engine does not depend on AI availability.
- Planning list processing and transaction ingestion emit domain events for triggers.
- Time synchronisation adequate to evaluate quiet hours and digests.

## Security and Privacy

- No PII leaves the home server; suggestion payloads contain only category identifiers and amounts.
- Audit entries are immutable and redact any PII in user metadata; access is role-restricted.

## Edge Cases

- End-of-month rollover and carryover rules.
- Pending transactions later re-categorised.
- Seasonal spikes (e.g., holidays) reduce false positives; confidence lowered or suppression applied.
- Tiny balances where reallocation is impractical; suggestions suppressed.
- Duplicate triggers coalesced into a single suggestion batch.
