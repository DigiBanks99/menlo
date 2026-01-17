# Requirement: Ralph Orchestration Loop

## Purpose

Establish an autonomous AI-driven development loop using the Ralph Wiggum technique that enables Claude Code to implement features, fix bugs, and evolve the Menlo codebase based on specifications and a living plan.

## Background

The Ralph Wiggum technique (ghuntley.com/ralph) is a proven approach for autonomous code generation:

```bash
while :; do cat PROMPT.md | claude-code ; done
```

Key principles:
- One task per loop (monolithic, not multi-agent)
- Deterministic context allocation (specs + plan loaded every loop)
- Subagents for parallelism (search/write parallel, validation single-threaded)
- Back pressure via tests/build/types (fast wheel turns matter)
- Self-improving AGENT.md (Ralph documents learnings)
- fix_plan.md as living TODO (regenerate when off rails)

## Dependencies

- Existing documentation structure in `/docs/requirements/` (read-only specs)
- Aspire AppHost for full-stack validation
- Claude Code CLI with Pro subscription
- Podman + devcontainer for sandboxed execution

## Business Requirements

- BR-1: Ralph must operate within a sandboxed environment to prevent accidental damage to the host system.
- BR-2: Ralph must read specifications from `/docs/requirements/` but never modify them (read-only ground truth).
- BR-3: Ralph must maintain a living plan in `/docs/plans/fix_plan.md` that tracks work to be done.
- BR-4: Ralph must validate all changes using Aspire before considering them complete.
- BR-5: Ralph must commit working changes and create PRs for review (no direct push to main).
- BR-6: Ralph must operate within Claude Pro subscription limits (conservative subagent usage).

## Functional Requirements

### FR-1: Devcontainer Sandbox

- Podman-compatible devcontainer configuration
- Includes: .NET 10 SDK, Node 24, pnpm, Aspire CLI, Claude Code CLI, Git, GitHub CLI
- No Ollama/AI models initially (lean container)
- Resource allocation: 16GB RAM, 8 cores maximum

### FR-2: AGENT.md (Runtime Context)

- Minimal file at repository root
- Contains: build commands, validation steps, where things live
- Self-improving: Ralph updates with learnings (keep brief, no status reports)
- Must include `aspire run` as primary validation command

### FR-3: Planning Loop (PROMPT_PLAN.md)

- Run manually to generate/regenerate fix_plan.md
- Studies existing code vs specifications
- Uses up to **5 parallel subagents** (Claude Pro conservative)
- Searches for: TODO, FIXME, NotImplementedException, placeholder implementations
- Outputs prioritized bullet list to `/docs/plans/fix_plan.md`

### FR-4: Building Loop (PROMPT_BUILD.md)

- Main continuous loop
- Chooses most important item from fix_plan.md
- Uses up to **5 parallel subagents** for search/write operations
- Uses exactly **1 subagent** for validation
- Bug handling workflow: document in fix_plan.md FIRST, then spawn subagent to fix

### FR-5: Validation Back Pressure

- Primary: `aspire run` - verify ALL resources report healthy
- Secondary: `dotnet build Menlo.slnx` - must pass
- Secondary: `dotnet test Menlo.slnx` - must pass
- Secondary: `pnpm --dir src/ui/web test:all` - must pass
- Secondary: `pnpm --dir src/ui/web lint` - must pass
- Do not proceed until all validation passes

### FR-6: fix_plan.md Maintenance

- Lives at `/docs/plans/fix_plan.md`
- Initially empty, populated by planning loop
- Updated by build loop as items complete
- Cleanup required when exceeding 100 items:
  - Remove completed items
  - Consolidate duplicates
  - Re-prioritize remaining work
- Use subagent for cleanup operations

### FR-7: Git Operations

- On green validation: `git add -A && git commit` with descriptive message
- Create PR for significant changes using `gh pr create`
- Never push directly to main
- Commit message must describe actual changes made

### FR-8: Loop Runner Script (ralph.sh)

- Mode selection: `plan` or `build`
- Plan mode: single execution to generate fix_plan.md
- Build mode: continuous loop with brief pause between iterations
- Uses `--dangerously-skip-permissions` flag for autonomous operation

## Non-Functional Requirements

- NFR-1: Subagent limits tuned for Claude Pro subscription (max 5 parallel for search/write, 1 for validation)
- NFR-2: Container must start within 2 minutes
- NFR-3: Single validation cycle should complete within 5 minutes
- NFR-4: AGENT.md must stay under 100 lines to preserve context window
- NFR-5: fix_plan.md should be kept actionable (under 100 items)

## Considerations

- Specs in `/docs/requirements/` are READ-ONLY ground truth - Ralph implements to spec, never changes specs
- The planning loop should be run sparingly (once per session, not every build loop)
- Expect 5-15 build loops per Claude Pro reset window depending on complexity
- When fix_plan.md goes completely off track, delete and regenerate via planning loop
- Trust eventual consistency - most issues resolve through more loops

## Acceptance Criteria

- AC-1: Devcontainer starts successfully with Podman and all tools are available
- AC-2: `ralph.sh plan` generates a non-empty fix_plan.md from current codebase state
- AC-3: `ralph.sh build` picks up items from fix_plan.md and attempts implementation
- AC-4: Validation fails prevent commits (back pressure works)
- AC-5: Successful changes result in git commit with descriptive message
- AC-6: AGENT.md is updated when Ralph discovers useful learnings
- AC-7: Subagent counts stay within Pro subscription limits (observable in logs)

## Out of Scope

- Multi-agent orchestration (Ralph is monolithic by design)
- Ollama/local AI integration (decide later)
- Direct push to main branch
- Modifying specification documents
- Running on API credits (tuned for Pro subscription)

## Diagrams

- Loop flow: `diagrams/loop-flow.md`
