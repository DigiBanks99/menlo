# Cloudflare Pages Frontend & Angular 21 Upgrade - Implementation Plan

## Purpose

Deliver a unified single-domain frontend (Cloudflare Pages) and upgrade the Angular application to v21 while ensuring local development with Aspire 13.0.1, updated CI/CD workflows
(ci.yml + cd-frontend.yml), hardened CORS, PWA integrity, and documentation alignment. This replaces Azure Static Web Apps hosting references with Cloudflare Pages Direct Upload via GitHub Actions.

## Guiding Principles

- Keep build & deploy vendor-neutral (pnpm + Angular CLI) for portability.
- Minimise blast radius: perform Angular upgrade + Aspire upgrade in isolated steps with checkpoint commits.
- Enforce single production origin earlier (CORS) to surface integration issues quickly.
- Preserve existing vertical slice architecture and Result pattern; this change is hosting + tooling only.

## High-Level Phases

1. Baseline Capture & Branching
2. Angular 21 Migration
3. Aspire Upgrade to 13.0.1
4. Cloudflare Pages Project Setup (Direct Upload mode)
5. CI Workflow Adjustments (ci.yml)
6. CD Frontend Workflow Replacement (cd-frontend.yml → Cloudflare Pages deploy)
7. PWA Validation & Performance Baseline
8. Documentation & Diagram Sweep
9. Test & Acceptance Validation
10. Rollback & Post-Deployment Monitoring

---

## Phase Details

### Phase 1: Baseline Capture & Branching

Tasks:

1. Create feature branch `feat/cloudflare-pages-angular21` off `main`.
2. Record current metrics locally: `pnpm run build:all:prod` duration, bundle sizes (main js, initial css), Lighthouse FCP.
3. Snapshot current AppHost Aspire SDK & package versions (`Menlo.AppHost.csproj`).
4. Grep for "Azure Static Web Apps" to confirm prior replacements list (ensure none outside historic ADR context remain after plan completion).
Artifacts: baseline.md stored in requirement folder (optional).

### Phase 2: Angular 21 Migration

Tasks:

1. In `src/ui/web` run: `pnpm exec ng version` to confirm current version.
2. Execute: `pnpm exec ng update @angular/core@21 @angular/cli@21` (handle prompts automatically; fail build on migration errors).
3. Update Node engine if Angular 21 requires newer version (validate against release notes; adjust NODE_VERSION env if needed).
4. Ensure strict configuration persists: check `tsconfig.json` & project tsconfigs (no loosening of compiler flags).
5. Replace deprecated control flow with built-in Angular control flow if not yet adopted (`@if`, `@for`, `@switch`)—audit templates.
6. Introduce at least one deferrable view for a heavier feature route (mark feature flagged if incomplete).
7. Adopt new hydration or SSR-ready flags only if currently in use (defer SSR full adoption – out of scope).
8. Update scripts in `package.json` if Angular 21 build output path semantics changed.
9. Re-run UI unit tests & lint after migration.
10. Add "Angular 21 Migration Notes" section to requirement specifications (optional cross-link).
Acceptance Checkpoints: Build succeeds; no deprecated API warnings; tests green.

### Phase 3: Aspire Upgrade to 13.0.1

Context: Current AppHost SDK version is 9.4.1; upgrade to align with Aspire 13.* improvements (service orchestration, diagnostics).
Tasks:

1. Install/upgrade Aspire CLI: `dotnet tool update -g aspire` (or initial install) targeting the 13.0.1 release.
2. Run `aspire upgrade` in repo root (or AppHost directory) to inspect recommended csproj changes.
3. Update `<Sdk Name="Aspire.AppHost.Sdk" Version="13.0.1" />` in `Menlo.AppHost.csproj`.
4. Pin explicit package versions if required (Aspire may add version suggestions for Hosting packages).
5. Validate service start locally: `dotnet run --project src/api/Menlo.AppHost` (ensure PostgreSQL & Ollama resources still resolve).
6. Confirm no breaking changes in environment variable or builder API usage.
7. Update documentation references to Aspire version (architecture-document technology section if version referenced).
Acceptance: Local orchestration unchanged; API & AI health endpoints respond; no build warnings about SDK mismatch.

### Phase 4: Cloudflare Pages Project Setup

Tasks:

1. Create Cloudflare Pages project (Direct Upload) – do not auto-connect repo for native builds.
2. Generate scoped API token (Pages Write) + retrieve Account ID; store as GitHub secrets: `CLOUDFLARE_API_TOKEN`, `CLOUDFLARE_ACCOUNT_ID`.
3. Determine `CLOUDFLARE_PAGES_PROJECT_NAME` (e.g., `menlo-ui`).
4. Configure custom domain `menlo.yourdomain.com` mapping (Pages dashboard) and verify DNS propagation.
5. Add SPA fallback configuration: create `_redirects` file with `/* /index.html 200` or use Pages UI setting.
6. Document manual deploy command (wrangler): `wrangler pages deploy dist/menlo-app --project-name $PROJECT --branch main`.
Acceptance: Domain resolves; 404 fallback rewrites to index.html; manual test build successful.

### Phase 5: CI Workflow Adjustments (ci.yml)

Changes:

1. Update NODE_VERSION if Angular 21 supports recommended Node LTS (verify release notes). Keep in env section.
2. Add step to produce `version.json` during frontend build (commit SHA, build timestamp) stored in `dist/menlo-app`.
3. Optionally add Angular cache priming (persist `.angular/cache` directory) for faster builds.
4. Remove Azure-specific aspects (none present here) – minimal changes; CI remains mostly same.
5. Add job output artifact `frontend-pages-dist` zipped for debugging deploy issues (optional).
6. Add a check for presence of `version.json` after build.
Acceptance: CI pipeline passes with Angular 21; artifact contains expected built assets & version metadata.

### Phase 6: CD Frontend Workflow Replacement (cd-frontend.yml)

Actions:

1. Rename workflow to `CD - Deploy Frontend to Cloudflare Pages`.
2. Replace Azure Static Web Apps token logic with Cloudflare secrets validation (`CLOUDFLARE_API_TOKEN`, `CLOUDFLARE_ACCOUNT_ID`, `CLOUDFLARE_PAGES_PROJECT_NAME`).
3. Remove generation of `staticwebapp.config.json` (Azure specific) and route rewrite logic—handled by Pages `_redirects` and tunnel separation.
4. Build steps remain (pnpm install + build:all:prod).
5. Add deploy step options:
   - Preferred: Use `cloudflare/wrangler-action@v3` with `wrangler pages deploy dist/menlo-app --project-name ${{ env.CLOUDFLARE_PAGES_PROJECT_NAME }} --branch main`.
   - Fallback: Direct REST API call (multipart upload) if wrangler action unavailable.
6. Add verification: curl production domain root; curl `/api/ai/health` (expect 200 with JSON).
7. Post-deploy summary includes Pages deployment ID & preview URL (if branch deploy).
8. Add preview deployment trigger for pull requests referencing updated environment branch name.
Acceptance: Deployment publishes new build; version.json accessible; health endpoint reachable with proper CORS headers.

### Phase 7: PWA Validation & Performance Baseline

Tasks:

1. Confirm service worker updated & registered (open dev tools Application panel after deploy).
2. Offline test: load site, then disable network and reload – verify cached shell & at least one budget list component view (stub if dataset not yet present).
3. Lighthouse run on Cloudflare production domain; record FCP, LCP metrics; compare to baseline.
4. Ensure hashed assets have long-lived cache headers (Cloudflare default OK; override only if missing immutability).
Acceptance: AC-CP-04 (offline), NFR-CP-02 (FCP < 2.5s), NFR-CP-03 (cache hit > 90% after initial usage—tracked later).

### Phase 8: Documentation & Diagram Sweep

Tasks:

1. Update `deployment/README.md` to reflect Cloudflare Pages workflow & wrangler usage.
2. Add section "Frontend Hosting Revision" referencing ADR-001 revision date.
3. Confirm architecture-document hosting section matches new CDN & Pages references (already partially updated).
4. Add note in business-requirements technical stack referencing Angular 21 & Cloudflare Pages (done, validate).
5. Cross-link requirement folder from README Implementation Roadmap if needed.
Acceptance: No active references to Azure Static Web Apps outside historic context blocks.

### Phase 9: Test & Acceptance Validation

Tasks:

1. Execute test cases TC-CP-01..16 sequentially.
2. Add quick script to assert CORS origin rejection for a random origin (`curl -H "Origin:https://evil.example"`).
3. Validate version.json content matches commit sha & timestamp.
4. Generate summary document `acceptance-report.md` (optional) in requirement folder.
Acceptance: All AC-CP-01..11 satisfied; failures logged with remediation plan.

### Phase 10: Rollback & Post-Deployment Monitoring

Tasks:

1. Document rollback: redeploy prior commit using wrangler branch flag or Pages dashboard previous build.
2. Add monitoring note: future addition of synthetic checks (GitHub Action scheduled ping + Lighthouse).
3. Set calendar reminder to rotate Cloudflare API token annually.
Acceptance: Rollback procedure documented; token rotation policy noted.

---

## GitHub Actions Workflow Changes Summary

### ci.yml

- Update NODE_VERSION if required by Angular 21.
- Add caching for Angular build cache `.angular/cache`.
- Inject version.json creation step before artifact upload.
- Validate presence of version.json.

### cd-frontend.yml

- Rename workflow name & job labels.
- Remove Azure-specific config generation; add `_redirects` creation if not committed.
- Use wrangler action for deploy; fallback direct API.
- Add environment secret validation for Cloudflare tokens.
- Add curl health checks (root + `/api/ai/health`).
- Add preview deployment on PR (branch-based).

---

## Angular 21 Feature Adoption (Initial Targets)

- Replace any remaining `*ngIf`, `*ngFor` with new control flow where practical.
- Introduce deferrable view for heavier feature route (planning or budget analysis page).
- Evaluate using new hydration improvements (defer until SSR considered).
- Prepare optional follow-up requirement for deeper Angular 21 enhancements (signals-based router data, etc.).

---

## Aspire Upgrade Considerations

- Verify compatibility of CommunityToolkit Aspire extensions with 13.0.1.
- Ensure new SDK does not change resource declarations (PostgreSQL/Ollama).
- Add health checks surfaced in AppHost explorer if new diagnostics appear.

---

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|-----------|
| Angular migration breaking build | High | Perform upgrade early; commit after tests green |
| Cloudflare token misconfiguration | Medium | Validate secrets job before deploy |
| CORS misconfig blocks production | High | Add automated curl origin test in deploy workflow |
| Aspire upgrade introduces API changes | Medium | Upgrade after Angular stabilization; test AppHost locally |
| Missing offline assets | Low | Add explicit precache list in service worker if needed |

---

## Rollback Strategy Summary

- Revert branch to pre-upgrade commit & redeploy via wrangler.
- Use Pages dashboard to promote previous successful build if immediate restoration needed.
- Reintroduce Azure SWA only as last-resort (document path but not automated).

---

## Implementation Checklist

- [ ] Branch created & baseline metrics captured
- [ ] Angular 21 upgrade applied & tests green
- [ ] Deferrable view introduced (at least one route)
- [ ] Control flow syntax modernized where safe
- [ ] Aspire SDK upgraded to 13.0.1 & local run validated
- [ ] Cloudflare Pages project + domain configured
- [ ] Secrets stored in GitHub (token, account id, project name)
- [ ] ci.yml updated (version.json, cache)
- [ ] cd-frontend.yml replaced (wrangler deploy + health checks)
- [ ] version.json deployed & accessible
- [ ] CORS production origin restricted and tested
- [ ] Offline/PWA test passed
- [ ] Performance baseline recorded (Lighthouse FCP)
- [ ] Documentation sweep completed (deployment README, architecture, ADR references)
- [ ] Acceptance test cases executed (TC-CP-01..16)
- [ ] Rollback procedure documented

---
See also: [Specifications](specifications.md), [Test Cases](test-cases.md), [Diagrams](diagrams/hosting-flow.md), [ADR-001](../../decisions/adr-001-hosting-strategy.md).
