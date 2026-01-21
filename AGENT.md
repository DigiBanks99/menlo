# Menlo - AGENT.md

## Quick Start
```bash
aspire run  # Starts full stack: PostgreSQL, API, Web UI
```
Wait for all resources to report healthy before proceeding.

## Validation Commands
```bash
# Must all pass before committing
aspire run                          # All resources healthy
dotnet build Menlo.slnx             # Build succeeds
dotnet test Menlo.slnx              # All tests pass
pnpm --dir src/ui/web test:all      # Frontend tests pass
pnpm --dir src/ui/web lint          # No lint errors
```

## Where Things Live
- **Backend**: `src/api/` (Menlo.Api, Menlo.AppHost) and `src/lib/` (Menlo.Lib, Menlo.AI)
- **Frontend**: `src/ui/web/projects/` (menlo-app, menlo-lib, data-access)
- **Specs**: `docs/requirements/` (READ-ONLY - do not modify)
- **Plan**: `docs/plans/fix_plan.md` (your working TODO list)

## Tech Stack
- .NET 10, C# 14, Entity Framework Core, PostgreSQL
- Angular 21, TypeScript, Vite, Vitest
- Aspire 13.1 for orchestration

## Learnings
<!-- Ralph updates this section with discoveries - keep brief -->

- **xUnit v3 tests**: Run test exe directly (e.g., `/tmp/menlo-build/Menlo.Lib.Tests/bin/Debug/net10.0/Menlo.Lib.Tests`) for visible output. `dotnet test` doesn't show xUnit v3 output properly.
- **Duplicate assembly errors**: If CS0579 errors appear, clean both `/tmp/menlo-build` AND local `src/**/obj` dirs: `find src -name obj -type d -exec rm -rf {} +`
- **API endpoint pattern**: Use extension methods on RouteGroupBuilder (C# 14 feature) - see `src/api/Menlo.Api/Budgets/Endpoints/` for examples.
