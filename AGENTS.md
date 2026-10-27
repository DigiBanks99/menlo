# Menlo - AGENT.md

Menlo is an AI-enhanced family home management application designed for a South African family of 5, focusing on budget management, planning coordination, and rental income analysis.

## Where Things Live

- **Backend**: `src/api/` (Menlo.Api, Menlo.AppHost) and `src/lib/` (Menlo.Lib, Menlo.AI)
- **Frontend**: `src/ui/web/projects/` (menlo-app, menlo-lib, data-access)
- **Specs**: `docs/requirements/` (READ-ONLY - do not modify unless told to draft new specs)
- **Repo**: <https://github.com/DigiBanks99/menlo>

## Tech Stack

- .NET 10, C# 12, Entity Framework Core, PostgreSQL
- Angular 21, TypeScript, Vite, Vitest
- Aspire for local dev loop
- Deployed via GitHub Actions to a Linux local server via CloudFlare tunnels (see ADR-004)

## Running

Use `aspire` to run the application

## Testing

- Use `aspire` and `playwright-cli` for interactive feature testing and validation
- Use `dotnet` for back-end unit and integration tests
- Use `pnpm` for front-end tests

## Linting and formatting

- Web:
  - Linting: `pnpm lint`
  - Formatting: `pnpm format`
- All .NET: `dotnet format`

## API Coverage

**Guardrail:** Changed C# files under `src/api/Menlo.Api/**` must stay at or above **70% line coverage** in CI. The repo-local guardrail definition lives in `scripts/` (implemented in a parallel lane — do not modify it here).

## Learnings

You must not be on the main branch. You may commit to an existing branch.
You must use conventional commits and tag the github issue you are working on in the body of the commit.
Update your learnings as you progress but keep them brief.

<!-- Agent updates this section with discoveries - keep brief -->

- Local GitHub Actions reproduction already has a baseline helper at `scripts/act-ci.ps1`, using `ghcr.io/catthehacker/ubuntu:act-latest` for `pull_request` runs.
- Household IDs in shared-fixture API tests must be unique across test classes to avoid cross-test contamination.
- Tailwind v4 in `src/ui/web` should be wired through PostCSS, and `@tailwindcss/forms` should use `strategy: "class"` to avoid reset regressions during the design-system rollout.
- Storybook foundations can preview Latte and Mocha together by scoping Menlo's semantic CSS variables on per-story containers instead of relying on global `html.dark`.
- `src/ui/web/projects/menlo-lib/package.json` must point `types` to `types/menlo-lib.d.ts`; otherwise Vite dev overlays report `TS2307` for `menlo-lib` imports even when the dist package exists.
- `mnl-page-shell` should own the router-driven scroll reset while `mnl-tab-bar` keeps both mobile and desktop nav DOM trees mounted so CSS alone controls the responsive switch.
- Angular partial-compilation builds for `menlo-lib` can only bind to protected/public component members from templates; private signals break `ng-packagr` builds.
- `pnpm test:e2e` reuses any existing dev server on port 4200; kill stale `nx serve menlo-app` listeners before rerunning Playwright if a Vite overlay appears from old type-resolution errors.
- `src/ui/web/projects/menlo-lib/src/index.ts` and `src/public-api.ts` need to stay aligned when adding new exported molecules, or Storybook/dev imports drift from the packaged surface.
- Design-system gradients that must work in app runtime, Storybook previews, and Vitest are safest when driven by shared theme CSS variables instead of `light-dark()`.
- `mnl-button` exposes routed CTA interactions through its `pressed` output, so components that need navigation should handle routing in the host component instead of trying to attach `routerLink` directly.
- `pnpm exec nx test menlo-app --coverage` can fully cover a migrated app slice while still failing branch-wide because `menlo-app` and `menlo-lib` both enforce 100% global coverage across pre-existing uncovered files.
- `mnl-form-layout` needs explicit `[mnlFormTitle]` and `[mnlFormActions]` projection slots with the middle content slot excluding them, or Angular's wildcard projection swallows the sticky action bar content.
- `ng-apexcharts` 2.4 ships a standalone `ChartComponent` (`apx-chart`), and Menlo chart helpers should keep `ng-apexcharts` plus `apexcharts` in `peerDependencies` when exporting typed chart utilities.
- Branch protection lives in `.github/rulesets/protected-branches.json` and requires the `Quality Gate` job by name; renaming that job or removing its `if: always()` blocks every merge.
- CI builds and pushes the arm64 image (`menlo-api:sha-<sha>`); CD only deploys by SHA after CI succeeds on `main`, and jobs downstream of a skippable job need explicit `needs.<job>.result` checks.
- The home-server deploy job runs on the self-hosted Linux runner labelled `menlo-deploy` and only for `main` on `DigiBanks99/menlo`; add that label to any replacement runner and keep its steps `shell: bash` (no pwsh on the host, see ADR-004).
- Update Aspire only with `aspire update`: it bumps the `Aspire.AppHost.Sdk` version in the AppHost csproj together with the `Aspire.*` packages. Dependabot ignores `Aspire.*` because it can't update the SDK. For build locks, follow the global `aspire-orchestration` skill: stop the AppHost first.
- Aspire 13.6 runs `postgres:18` locally with the volume at `/var/lib/postgresql`; a `postgres-menlo` volume from PG 17 must be dumped and restored (an image tag change alone fails). Prod compose stays pinned to `postgres:17` separately.
- `menlo-app` resolves `data-access-menlo-api` from `dist/` (tsconfig paths) and `nx serve` doesn't rebuild it; if `web-ui` is unhealthy with `TS2305` missing exports, run `pnpm exec nx build data-access-menlo-api` and restart the resource.
- Production CSP has no `script-src 'unsafe-inline'` (only Development, for Scalar), so `menlo-app` production builds set `optimization.styles.inlineCritical: false`; Angular's critical-CSS `onload` handler would be blocked. `style-src` keeps `unsafe-inline` for Angular runtime styles.
