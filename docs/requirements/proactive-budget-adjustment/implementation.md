# Proactive Budget Adjustments — Implementation Plan

## Overview

Deliver a suggestions pipeline that observes triggers, generates explainable options locally, and surfaces a safe, auditable UX for approval.

## Steps

1. Domain Model

- Add Suggestion aggregate: id, month, reason code, confidence, affected categories (source/target), amounts, rationale, createdBy/createdAt, status (New, Accepted, Dismissed, Snoozed, Modified), undo pointer.
- Add Category metadata: locked (bool), minimum (Money), earmarked (Money/labels), attribution/tax flags.
- Add Allocation ledger to record before/after entries for accepted changes.

1. Config and Feature Toggle

- Per-category thresholds, sensitivity, seasonal flags.
- Global toggle, persona delivery options, quiet hours, digest schedule.

1. Trigger Detection

- Threshold watcher: evaluates allocation vs spend periodically (e.g., 1–5 min).
- List impact watcher: listens for list processing events and maps costs to categories.
- Trend deviation: rolling historical/seasonal comparator (per week-of-month).
- Event proximity: upcoming calendar events with budget links.

1. Suggestion Generation

- Rules engine assembles 1–3 option sets (Conservative/Balanced/Aggressive) obeying locks/minimums and net-zero by default.
- Rank via local LLM (Ollama, Semantic Kernel) with structured prompt; include reason code, confidence, and concise rationale.
- Deduplicate by hashing the contributing signals within a time window.

1. APIs

- Queries: list suggestions by month/category/status; get details and rationale.
- Commands: accept, modify, dismiss, snooze, undo.
- Audit: list allocation ledger entries.

Contract notes:

- Inputs are identifier-based; no PII included in payloads.
- All commands idempotent; accept/modify guarded by current status to prevent double-apply.

1. UI (PWA)

- Budget Suggestions inbox, item detail with explanation and options.
- Accept/Modify/Dismiss/Snooze/Undo workflows.
- Settings for thresholds, sensitivity, quiet hours, persona delivery.

1. Notifications

- Emit suggestion-available events to the Notification System with user prefs (quiet hours/digest).

1. Observability

- Local metrics: counts by trigger/outcome, latency, dedup rate. No PII.

1. Degradation

- If AI offline, show banner and allow manual reallocations; queue trigger events for later processing with idempotency.

## Dependencies

- Ollama (local models, e.g., Phi-4-mini) and Microsoft Semantic Kernel for ranking/explanations.
- Event sources: planning list processor, transaction ingestion, calendar service.
- Time service for quiet hours and digests.

## Data Model Notes

- Suggestion drafts reference categories by stable identifiers.
- Allocation ledger stores before/after snapshots and a reason code; undo references the original snapshot.

## Rollout and Feature Toggle

- Ship behind a global feature flag; default Off.
- Progressive exposure by persona and category groups.

## Risks and Mitigations

- False positives: Seasonal flags, confidence thresholds, user feedback loop.
- User trust: Clear “Why” explanations and strict constraints; Undo always available.
- Privacy: Strict local-only processing with documented verification.

## Done When

- All acceptance criteria in ./specifications.md are met
- All tests in ./test-cases.md pass in integration scenarios
- Diagrams are updated and linked
- Documentation cross-referenced from business requirements (link only)
