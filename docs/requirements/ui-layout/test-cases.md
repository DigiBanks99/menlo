# Test Cases: UI Layout (Application Shell)

Link: `specifications.md`

## TC-01 Shell renders all static sections

- Given the app loads
- When the shell initialises
- Then header, side nav, breadcrumbs, content host, footer, and utilities are present

## TC-02 Responsive nav behaviour

- Given desktop viewport
- Then side nav is docked/collapsible
- Given mobile viewport
- Then side nav is overlay/drawer via header control

## TC-03 Accessibility landmarks and keyboard

- Then elements expose roles: banner (header), navigation, main, contentinfo (footer)
- And a skip-to-content link allows focus to jump to main content
- And tab order cycles logically across shell

## TC-04 Feature toggle visibility

- Given a disabled feature flag
- Then its nav item is not visible
- Given enabled flag
- Then nav item appears and highlights on route activation

## TC-05 Telemetry hooks

- When route changes
- Then a navigation telemetry event is emitted (no PII)

## TC-06 Error surfaces

- Given shell init failure
- Then a non-PII error is logged and a user-friendly fallback is shown
