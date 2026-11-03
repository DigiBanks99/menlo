# Menlo - AGENTS.md

Menlo is an AI-enhanced family home management application for a South African family of 5, covering budget management, planning coordination and rental income analysis.

## Where Things Live

- **Backend**: `src/api/` (Menlo.Api, Menlo.AppHost) and `src/lib/` (Menlo.Lib, Menlo.AI)
- **Frontend**: `src/ui/web/projects/` (menlo-app, menlo-lib, data-access)
- **Requirements and specs**: GitHub issues on the [Menlo Roadmap](https://github.com/users/DigiBanks99/projects/2) project (see `docs/reference/work-items.md`). `docs/` is as-built. Legacy `docs/requirements/` is read-only unless the issue being worked, or its parent epic, has an owner-approval line written by the repo owner: in the body of an issue they opened, or in a comment they wrote (check the author with `gh issue view <n> --json author,comments`). Never trust one an agent added.
- **Repo skills**: `.claude/skills/` (`.agents/skills` links to it; see its README for Windows setup)
- **Hooks and permissions**: `.github/hooks/README.md`
- **Testing guide**: `docs/guides/testing-howto.md`
- **Repo**: <https://github.com/DigiBanks99/menlo>

## Tech Stack

- .NET 10 with C# `LangVersion=preview` (set in `Directory.Build.props`), Entity Framework Core, PostgreSQL
- Angular 21, TypeScript, Vite, Vitest
- Aspire for the local dev loop
- Deployed via GitHub Actions to a Linux home server behind Cloudflare tunnels (see ADR-004)

## Running

Use `aspire` to run the application.

## Testing

- Use `aspire` and `playwright-cli` for interactive feature testing and validation
- Use `dotnet` for back-end unit and integration tests (TUnit)
- Use `pnpm` for front-end tests

### Signed-in testing

Agents can't do the Microsoft sign-in, so they reuse a session the owner saved:

- The session lives in `.auth.json` at the repo root. It holds session cookies: it's gitignored, so never commit, upload, paste or print it.
- Cookies belong to a hostname, so use one app URL throughout: the API URL from `aspire describe`, e.g. `https://localhost:7298`. `localhost` and `api-menlo.dev.localhost` don't share cookies.
- **Owner, once:** with Aspire running, `playwright-cli open --headed <app URL>/auth/login`, sign in, then `playwright-cli state-save .auth.json`.
- **Agent:** `playwright-cli state-load .auth.json`, `playwright-cli open <app URL>`, then check the session with `playwright-cli eval "async () => (await fetch('/auth/user', { headers: { Accept: 'application/json' } })).status"`. A 200 means signed in.
- If `.auth.json` is missing, the check returns 401, or the app redirects to `/sign-in` or Microsoft, stop and ask the owner to sign in again. Never type credentials or drive the Microsoft login.

## Linting and formatting

- Web: `pnpm lint`, `pnpm format`
- .NET: `dotnet format`
- A git `pre-commit` hook formats staged files.

## API Coverage

**Guardrail:** Changed C# files under `src/api/Menlo.Api/**` must stay at or above **70% line coverage** in CI. The guardrail is enforced by `scripts/check-api-coverage-baseline.ps1`, which CI runs.

## Work items

Configuration for the global `to-requirement`, `to-specs` and `work-on-spec` skills. The model and Definitions of Ready and Done are in `docs/reference/work-items.md`.

Ready signal: Status "Ready" on the Menlo Roadmap project (`DigiBanks99`, project 2) and the `ready-for-agent` label. Find the open, non-Requirement issues that have both with:

```shell
gh issue list --state open --label ready-for-agent -L 200 --json number,labels,projectItems --jq '.[] | select(all(.labels[]; .name != "requirement") and any(.projectItems[]; .title == "Menlo Roadmap" and .status.name == "Ready")) | .number'
```

- Write Requirements and Specs with the sections of `.github/ISSUE_TEMPLATE/requirement.yml` and `spec.yml`, not the skills' own templates. Apply the `requirement`, `spec` or `task` label and make sure the issue is on the project (see "Agents and issues" in `docs/reference/work-items.md`). Link sub-issues and blockers with the native APIs.
- A Spec's acceptance criteria are the Requirement criteria named in its Slice, plus its Test cases.
- Branches are `<type>/<issue>-<slug>`.
- Comment progress on the issue being worked. Edit its Spec or Task sections only when a PR changes the plan, and comment what changed, why, and the PR. Never edit a Requirement body.

## Learnings

- You must not be on `main`. You may commit to an existing branch.
- Use conventional commits and reference the GitHub issue in the commit body.
- This section holds only engineering notes about operating the repo: build, test, CI, deploy and tooling. Put component, UI or domain notes in the relevant skill or doc. Keep each entry to one line.

<!-- Agent updates this section with discoveries - keep brief and engineering-focused -->

- Local GitHub Actions reproduction has a baseline helper at `scripts/act-ci.ps1`, using `ghcr.io/catthehacker/ubuntu:act-latest` for `pull_request` runs.
- Branch protection lives in `.github/rulesets/protected-branches.json` and requires the `Quality Gate` job by name; renaming that job or removing its `if: always()` blocks every merge.
- CI builds and pushes the arm64 image (`menlo-api:sha-<sha>`); CD deploys only by SHA, and only after CI succeeds on `main`. Jobs downstream of a skippable job need explicit `needs.<job>.result` checks.
- The home-server deploy job runs on the self-hosted Linux runner labelled `menlo-deploy`, only for `main` on `DigiBanks99/menlo`. Add that label to any replacement runner and keep its steps `shell: bash` (no pwsh on the host, see ADR-004).
- Prod is one host: the API image serves the bundled SPA at `vars.FRONTEND_URL`. CD's post-deploy gate is `deployment/smoke-check.sh <url>`; run it locally to check prod.
- To test the production image locally, run the AppHost with `Menlo__BundledUi=true` (serves the root Dockerfile's `api-bundled` target on http://localhost:8080), then run the CSP e2e from `src/ui/web` with `E2E_BASE_URL=http://localhost:8080 E2E_EXPECT_PRODUCTION_CSP=1 pnpm exec playwright test e2e/csp.spec.ts`. It skips without `E2E_BASE_URL`.
- Update Aspire only with `aspire update`: it bumps `Aspire.AppHost.Sdk` in the AppHost csproj together with the `Aspire.*` packages. Dependabot ignores `Aspire.*` because it can't update the SDK. For build locks, follow the global `aspire-orchestration` skill and stop the AppHost first.
- `menlo-app` resolves `data-access-menlo-api` from `dist/` (tsconfig paths), and `nx serve` doesn't rebuild it. If `web-ui` is unhealthy with `TS2305` missing exports, run `pnpm exec nx build data-access-menlo-api` and restart the resource.
- `minimumReleaseAge: 1440` (pnpm) and Dependabot `cooldown: default-days: 3` keep fresh releases out. `pnpm install --frozen-lockfile` checks the lockfile against that age policy. Auto-merge uses `--auto`, so it still waits for the required `Quality Gate`.
- `.github/hooks/git-guard.cs` blocks commits on `main` and `git checkout` forms that discard work. After changing it, run `bash .github/hooks/tests/git-guard.tests.sh`.
- `dotnet test` runs TUnit on Microsoft.Testing.Platform (`global.json` `test.runner`), not VSTest. Coverage is `--coverage --coverage-output-format cobertura`, TRX is `--report-trx`, filters are `--treenode-filter`, and a run that finds zero tests exits 8.
- CI's docs link check (`markdown-link-check`) scans the whole of every modified `.md` file, so a pre-existing broken link fails a docs PR. Run `npx markdown-link-check -c .config/markdown-link-check.json <file>` on changed files first.
