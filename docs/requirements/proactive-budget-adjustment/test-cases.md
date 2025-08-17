# Proactive Budget Adjustments — Test Cases

Link: ./specifications.md

## Happy Path

1) Given Groceries crosses 80% mid-month
- When the threshold is detected
- Then a suggestion appears within 5 minutes with at least one net-zero option
- And the option shows source category, target category, amounts, confidence, and reason code THRESHOLD_REACHED
- And the user can Accept to apply and create an audit entry

2) Given a completed meal-planning list adds expected grocery costs
- When list processing completes
- Then a suggestion is created referencing LIST_IMPACT with rationale and mapped categories
- And Modify lets the user tweak amounts before acceptance

## Constraints and Protections

3) Locked category is proposed as a source
- When generating options
- Then the locked category is excluded
- And no option breaches category minimums or earmarked funds

4) Rental vs personal attribution preserved
- When a suggestion moves funds in categories with splits
- Then attribution and tax-deductible flags remain intact
- And the suggestion explanation surfaces any tax implications

## Auditability and Undo

5) Accept creates an audit record
- When user Accepts
- Then an immutable entry records before/after amounts, user, timestamp, and reason code
- And Undo restores the exact prior state

## Notification Behaviour

6) Notification delivery
- Given quiet hours enabled
- When suggestions are generated during quiet hours
- Then only a digest is sent at the configured time
- And the suggestion inbox shows the items immediately

## Performance and Degradation

7) AI offline
- When AI services are unavailable
- Then no new suggestions are generated
- And UI indicates degraded mode while allowing manual reallocations

8) Suggestions inbox
- When opening the inbox on a typical home network
- Then it loads within 3 seconds with the latest items

## Deduplication and Seasonality

9) Duplicate triggers
- Given repeated transactions cause the same pressure within 15 minutes
- When generating suggestions
- Then the system coalesces into a single batch suggestion

10) Seasonal budget expectations
- Given a category marked seasonal (e.g., Holidays)
- When spending is higher but expected for the season
- Then confidence is reduced or suggestion suppressed according to settings

## Configuration and Security

1. Per-category thresholds and locks

- Given per-category overrides are configured
- When thresholds are crossed
- Then suggestions respect the overrides and locked/minimum constraints

1. Per-trigger opt-out

- Given a user opted out of TREND_DEVIATION for Groceries
- When a trend deviation occurs
- Then no notification is sent, but the suggestion appears in the inbox

1. Audit immutability and redaction

- When an audit record is created
- Then it is immutable and contains no PII in stored metadata
