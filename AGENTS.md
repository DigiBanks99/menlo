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
- Deployed via GitHub Actions to a Windows local server via CloudFlare tunnels

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

## API Coverage Baseline (`src/api/Menlo.Api`)

Measured from the latest full `Menlo.Api.Tests` run (post-fix, feat/285 branch).

| File | Line coverage |
|------|--------------|
| BudgetSummaryDto.cs | 100.00% |
| GetBudgetSummaryHandler.cs | 96.81% |
| FillForwardHandler.cs | 91.04% |
| BulkCreateBudgetItemHandler.cs | 91.18% |
| CreateBudgetItemHandler.cs | 91.55% |
| DeleteBudgetItemHandler.cs | 95.00% |
| ListBudgetItemsHandler.cs | 96.00% |
| BudgetItemMapper.cs | 94.59% |
| BudgetEndpoints.cs | 100.00% |
| BudgetItemDto.cs | 100.00% |
| BudgetItemEndpoints.cs | 100.00% |
| RecordItemSpentHandler.cs | 82.76% |
| RealizeItemHandler.cs | 82.76% |
| UpdateBudgetItemHandler.cs | 72.62% |
| **Overall `Menlo.Api.Tests` line-rate** | **75.53%** |

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
