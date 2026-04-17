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

## Learnings

You must not be on the main branch. You may commit to an existing branch.
You must use conventional commits and tag the github issue you are working on in the body of the commit.
Update your learnings as you progress but keep them brief.

<!-- Agent updates this section with discoveries - keep brief -->
- Local GitHub Actions reproduction already has a baseline helper at `scripts/act-ci.ps1`, using `ghcr.io/catthehacker/ubuntu:act-latest` for `pull_request` runs.
