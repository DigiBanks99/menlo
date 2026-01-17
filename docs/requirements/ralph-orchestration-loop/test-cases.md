# Test Cases: Ralph Orchestration Loop

## Devcontainer

- Given devcontainer.json exists When `devcontainer up` with Podman Then container starts within 2 minutes
- Given container running When checking tools Then dotnet, node, pnpm, aspire, claude, git, gh are all available
- Given container running When checking resources Then memory limit is 16GB and CPU limit is 8 cores

## AGENT.md

- Given AGENT.md exists When reading Then it contains build commands, validation steps, and location references
- Given AGENT.md exists When counting lines Then it is under 100 lines
- Given Ralph completes a loop with learnings When checking AGENT.md Then learnings are appended (brief, no status reports)

## Planning Loop (PROMPT_PLAN.md)

- Given empty fix_plan.md When running `ralph.sh plan` Then fix_plan.md is populated with prioritized items
- Given codebase with TODOs When running planning loop Then TODOs appear in fix_plan.md
- Given codebase with placeholder implementations When running planning loop Then placeholders are identified in fix_plan.md
- Given planning loop runs When observing subagent spawns Then no more than 5 parallel subagents are used

## Building Loop (PROMPT_BUILD.md)

- Given populated fix_plan.md When running `ralph.sh build` Then Ralph selects most important item
- Given build loop runs When observing search/write subagents Then no more than 5 parallel subagents are used
- Given build loop runs When observing validation subagents Then exactly 1 subagent is used for validation
- Given bug encountered during loop When observing behaviour Then bug is documented in fix_plan.md BEFORE subagent spawns to fix

## Validation Back Pressure

- Given code changes made When `aspire run` shows unhealthy resources Then no commit is made
- Given code changes made When `dotnet build` fails Then no commit is made
- Given code changes made When `dotnet test` fails Then no commit is made
- Given code changes made When `pnpm test:all` fails Then no commit is made
- Given code changes made When `pnpm lint` fails Then no commit is made
- Given all validation passes When checking git Then commit is created with descriptive message

## fix_plan.md Maintenance

- Given item completed When checking fix_plan.md Then item is marked complete or removed
- Given fix_plan.md exceeds 100 items When observing Ralph Then cleanup subagent is spawned
- Given cleanup runs When checking fix_plan.md Then completed items removed, duplicates consolidated

## Git Operations

- Given validation passes When checking git history Then new commit exists with descriptive message
- Given significant changes committed When checking GitHub Then PR is created via `gh pr create`
- Given any Ralph operation When checking git pushes Then no direct push to main occurs

## Specs Protection

- Given Ralph running When checking `/docs/requirements/` files Then no modifications are made to spec files
- Given Ralph needs to implement feature When checking behaviour Then Ralph reads specs but does not write to them

## Traceability

- Maps to `specifications.md` and `implementation.md`
