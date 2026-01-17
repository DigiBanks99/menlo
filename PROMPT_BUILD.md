Study @docs/requirements/* for specifications (READ-ONLY, do not modify).
Study @docs/plans/fix_plan.md for current plan.
Study @AGENT.md for build/test commands and learnings.

Your task: Choose the most important item from fix_plan.md and implement it.

## Rules

### Searching
- Search before assuming not implemented (use parallel subagents, max 5)
- Do not duplicate existing functionality

### Implementation
- Use up to 5 parallel subagents for searching and writing
- Use exactly 1 subagent for validation operations

### Bug Handling (CRITICAL)
- When you encounter ANY bug or issue: FIRST document it in @docs/plans/fix_plan.md
- THEN spin up a subagent to fix it
- Never attempt to fix without documenting first

### Validation (Back Pressure)
Run these in order, stop if any fail:
1. `aspire run` - verify ALL resources report healthy (PostgreSQL, API, Web)
2. `dotnet build Menlo.slnx` - must pass
3. `dotnet test Menlo.slnx` - must pass
4. `pnpm --dir src/ui/web test:all` - must pass
5. `pnpm --dir src/ui/web lint` - must pass

Do not proceed to git operations until ALL validation passes.

### Plan Maintenance
- Mark items complete in fix_plan.md when done (use subagent)
- When fix_plan.md exceeds 100 items, use a subagent to clean it up:
  - Remove completed items
  - Consolidate duplicate/related items
  - Re-prioritize remaining work

### Learnings
- Update @AGENT.md with useful learnings (keep brief, no status reports)
- Only add genuinely useful information for future loops

### Git (Only After All Validation Passes)
- `git add -A && git commit -m "descriptive message"`
- For significant changes: `gh pr create --title "..." --body "..."`
- Never push directly to main

### Specs Protection
- Files in @docs/requirements/* are READ-ONLY
- Implement TO the specs, never modify them
- If specs seem wrong, document the discrepancy in fix_plan.md for human review
