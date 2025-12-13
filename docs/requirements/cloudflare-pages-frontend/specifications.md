# Cloudflare Pages Frontend & Angular 21 Upgrade - Specifications

## Context

This requirement revises the original frontend hosting strategy (Azure Static Web Apps) to adopt **Cloudflare Pages** while upgrading the UI codebase to **Angular 21**.
It unifies delivery under the apex domain `menlo.yourdomain.com` with the existing Cloudflare Tunnel–exposed API (`/api`).
It removes cross-origin complexity and enables stricter production CORS.
Angular 21 improvements (signals refinements, deferrable views, faster builds, hydration enhancements) are leveraged.
Documentation updates are centralised here.

## Business Requirements (BR)

- **BR-CP-01 Single Domain Experience**: Family users access UI and API under one domain without perceptible cross-origin friction.
- **BR-CP-02 Low-Cost Delivery**: Hosting remains within the existing low-cost hybrid model (no new paid tiers required).
- **BR-CP-03 Privacy Preservation**: No change to privacy posture; all dynamic + AI processing still local.
- **BR-CP-04 Operational Simplicity**: Deployment flow for frontend must be observable and rollback-friendly from GitHub.
- **BR-CP-05 Performance Enhancement**: Angular 21 upgrade should reduce build time and improve initial load metrics.
- **BR-CP-06 Documentation Consistency**: All references to Azure Static Web Apps replaced with Cloudflare Pages across architecture and concept docs.

## Functional Requirements (FR)

- **FR-CP-01 Cloudflare Pages Project**: Create Pages project linked to GitHub repository main branch.
- **FR-CP-02 Build Command**: Use `pnpm install && pnpm exec ng build --configuration=production` with Angular 21.
- **FR-CP-03 Environment Separation**: Provide preview builds for feature branches using Cloudflare Pages previews.
- **FR-CP-04 SPA Fallback**: Configure single-page application routing (fallback to `index.html`).
- **FR-CP-05 Asset Caching**: Enable immutable caching for hashed Angular build assets; HTML not cached aggressively.
- **FR-CP-06 Domain Binding**: Bind `menlo.yourdomain.com` to Pages; ensure `/api/*` continues through tunnel.
- **FR-CP-07 CORS Policy Update**: Production API restricts `Origin` to `https://menlo.yourdomain.com`; development allows localhost origins.
- **FR-CP-08 Angular 21 Upgrade**: Migrate dependencies, adjust builder configs, validate strict mode remains enabled.
- **FR-CP-09 PWA Integrity**: Service worker updated / regenerated; offline core features (list viewing, cached budgets) remain functional.
- **FR-CP-10 Observability**: Record deployment metadata (commit, timestamp) surfaced in an about/version endpoint or static JSON.
- **FR-CP-11 Rollback Procedure**: Ability to redeploy previous commit via Cloudflare Pages UI or API documented.
- **FR-CP-12 Security Headers**: Confirm existing API security headers unaffected and UI served with modern headers (Content-Security-Policy to be added later – deferred).
- **FR-CP-13 Documentation Sweep**: Update: ADR-001, architecture-document, concepts-and-terminology, business-requirements, diagrams, README.
- **FR-CP-14 Testing Adjustments**: E2E tests configured to run against Pages preview URL for PR validation.
- **FR-CP-15 Angular 21 Features Adoption**: Leverage deferrable views / improved hydration where applicable (deferred task—placeholder).
- **FR-CP-16 GitHub Actions Deployment**: Use GitHub Actions to build Angular 21 artifacts and deploy to Cloudflare Pages via Direct Upload API (supports production and preview environments).

## Non-Functional Requirements (NFR)

- **NFR-CP-01 Build Duration**: Cold build ≤ 90 seconds on Cloudflare Pages.
- **NFR-CP-02 First Contentful Paint**: Target < 2.5s on typical SA home broadband.
- **NFR-CP-03 Cache Hit Ratio**: > 90% for static assets after 1 day of normal usage.
- **NFR-CP-04 Uptime**: Frontend availability ≥ 99% (Pages SLA + Tunnel reliability).
- **NFR-CP-05 No Added Vendor Lock-In**: Migration path back to alternative static hosting retained (document build neutrality).

## Acceptance Criteria (AC)

- **AC-CP-01**: Cloudflare Pages deployment succeeds and serves Angular 21 build at `https://menlo.yourdomain.com`.
- **AC-CP-02**: API requests from UI occur without CORS preflight failures; production CORS only allows the apex domain.
- **AC-CP-03**: All Azure Static Web Apps references removed or marked historic; replaced with Cloudflare Pages.
- **AC-CP-04**: Service worker functions post-upgrade (verified offline loading of at least one cached budget list and UI shell).
- **AC-CP-05**: Build logs accessible with commit SHA and timestamp; rollback procedure documented.
- **AC-CP-06**: E2E smoke test passes against Pages preview and production deployment.
- **AC-CP-07**: Angular 21 features compile successfully; no deprecated API warnings remain.
- **AC-CP-08**: Security headers for API unchanged / valid; no regression in existing header policy tests.
- **AC-CP-09**: Performance metrics baseline captured (build time, FCP) and documented.
- **AC-CP-10**: Diagram updates merged (context, component) reflecting new hosting path.
- **AC-CP-11**: GitHub Actions workflow builds + uploads site; production deploy shows matching commit SHA and preview deploy triggers on PR.

## Traceability

| Item | Source | Artifact |
|------|--------|----------|
| BR-CP-01..06 | Hosting revision request | ADR-001 (revised), this spec |
| FR-CP-01..07 | Domain + deployment needs | Cloudflare Pages docs, Tunnel existing setup |
| FR-CP-08..15 | Angular upgrade + DX | Angular 21 release notes |
| AC-CP-01..10 | Verification layer | Test cases / CI configuration |

## Dependencies / Risks

- Cloudflare Pages build image Node version must be compatible with Angular 21 & pnpm.
- Potential CSP tightening deferred—risk of future mixed content if AI endpoints expose additional assets.
- Single domain approach removes visible CORS complexity but mandates robust API rate limiting in future.

---

See also: [Implementation Plan](implementation.md), [Test Cases](test-cases.md), [Diagrams](diagrams/hosting-flow.md), [ADR-001](../../decisions/adr-001-hosting-strategy.md).
