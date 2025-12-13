# Cloudflare Pages Frontend & Angular 21 Upgrade - Test Cases

Each test maps to Acceptance Criteria (AC) and Functional Requirements (FR) in `specifications.md`.

## Deployment & Hosting

- [ ] **TC-CP-01 (AC-CP-01, FR-CP-01/02)**: Cloudflare Pages build completes; site loads root HTML with status 200.
- [ ] **TC-CP-02 (AC-CP-02, FR-CP-07)**: API call from UI includes Origin header `https://menlo.yourdomain.com` and passes CORS; mismatched origin denied.
- [ ] **TC-CP-03 (FR-CP-04)**: Direct navigation to deep route resolves via SPA fallback.
- [ ] **TC-CP-04 (FR-CP-06)**: Domain binding verified (DNS & SSL valid) – no certificate warnings.

## Angular 21 Upgrade

- [ ] **TC-CP-05 (AC-CP-07, FR-CP-08)**: Build logs show Angular 21 versions; no migration warnings remain.
- [ ] **TC-CP-06 (FR-CP-15)**: Feature branch demonstrates one deferrable view usage (placeholder acceptance – optional).

## PWA & Caching

- [ ] **TC-CP-07 (AC-CP-04, FR-CP-09)**: Offline test: after initial load, disconnect network, reload – UI shell & cached data available.
- [ ] **TC-CP-08 (NFR-CP-03, FR-CP-05)**: Check response headers for hashed assets: `Cache-Control: public, max-age>2592000, immutable`.

## Observability & Rollback

- [ ] **TC-CP-09 (AC-CP-05, FR-CP-10/11)**: `version.json` accessible and rollback procedure documented.
- [ ] **TC-CP-10 (AC-CP-06, FR-CP-14)**: E2E smoke test hits Pages preview URL + `/api/ai/health` returns expected JSON.

## Documentation & Diagrams

- [ ] **TC-CP-11 (AC-CP-03, FR-CP-13)**: Grep reveals no active non-historic references to Azure Static Web Apps.
- [ ] **TC-CP-12 (AC-CP-10)**: Updated diagrams committed and render new hosting path.
- [ ] **TC-CP-16 (AC-CP-11, FR-CP-16)**: GitHub Actions run shows successful deploy step; artifact served at production domain with matching commit hash; PR run produces preview URL.

## Performance

- [ ] **TC-CP-13 (NFR-CP-01)**: Build duration ≤ 90s (recorded from Pages log).
- [ ] **TC-CP-14 (NFR-CP-02)**: Lighthouse / Web Vitals: FCP < 2.5s on representative SA connection profile.

## Security

- [ ] **TC-CP-15 (AC-CP-08, FR-CP-12)**: Validate API security headers unchanged (compare before/after snapshot).

---

See also: [Specifications](specifications.md), [Implementation Plan](implementation.md), [Diagrams](diagrams/hosting-flow.md).
