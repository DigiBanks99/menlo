---
name: github-actions-act-debug
description: Use this skill whenever the user wants to debug a GitHub Actions failure locally, reproduce a failing workflow or job with nektos act, compare GitHub runner behavior with the local environment, or keep iterating on workflow/code changes until the failure is understood and fixed locally. Trigger on requests about broken CI/CD runs, flaky workflows, failing GitHub jobs, local workflow reproduction, act setup, or narrowing a GitHub pipeline failure down to the exact failing step.
---

# GitHub Actions Debugging With act

This repository already uses GitHub Actions heavily and has a local helper script at `scripts/act-ci.ps1` for the main CI path. Use this skill to move from "a GitHub run failed" to "the failure is reproduced locally, debugged, and either fixed or isolated with clear next steps."

## Goal

Work the problem in this order:

1. Identify the exact failing workflow, job, step, event, branch, and commit.
2. Reproduce the failure locally with the smallest viable `act` command.
3. Debug until the failure is explained.
4. Fix the workflow or product code when the root cause is local and actionable.
5. Rerun locally until the reproduction passes or the remaining gap is clearly external to `act`.

Do not stop at "I found the workflow." The point of this skill is local reproduction and iterative debugging.

## When To Reach For This Skill

Use this skill for prompts like:

- "CI is failing on GitHub, reproduce it locally."
- "Use act to debug why this workflow broke."
- "The `cd-frontend` job fails in Actions but I want to fix it from my machine."
- "A PR check is red, figure out the exact failing step and get it green locally."

Do not use this skill for general GitHub Actions authoring when there is no failure to investigate. It is specifically for failure analysis, local reproduction, and iterative debugging.

## Repo-Specific Starting Points

- Workflows live in `.github/workflows/`.
- The repo already includes `scripts/act-ci.ps1`, which runs:

```powershell
act pull_request -P ubuntu-latest=ghcr.io/catthehacker/ubuntu:act-latest
```

- Use that script first for broad CI reproduction when the failure is in the main CI workflow.
- For deployment workflows such as `cd-frontend.yml` and `cd-backend.yml`, prefer a direct `act` invocation scoped to the specific workflow and job.

## Standard Workflow

### 1. Scope the failure precisely

Before running anything locally, gather the smallest set of facts that determine the reproduction:

- Workflow file name
- Job name
- Step name or failing command
- Event type: `push`, `pull_request`, `workflow_dispatch`, `schedule`, tag push, or reusable workflow call
- Branch or ref
- Commit SHA if known
- Whether the failure depends on secrets, vars, environments, artifacts, caches, or external services

If the user gave a run URL, run number, or screenshot, translate that into the workflow file and failing job. Read the workflow YAML before guessing at the command.

### 2. Read the workflow and its dependencies

Inspect:

- The target workflow in `.github/workflows/`
- Any reusable workflows it calls
- Scripts invoked by steps
- Package manager or build files referenced by the failing step

Look for:

- Path filters that decide whether the workflow should run
- `if:` conditions that differ by event or branch
- Secrets, vars, and environment expressions
- Action versions and runner assumptions
- Shell differences between GitHub and the local host

### 3. Choose the narrowest useful `act` command

Prefer a targeted run over a full workflow when you already know the failing job.

Common patterns:

```powershell
act pull_request -W .github/workflows/ci.yml -j detect-changes -P ubuntu-latest=ghcr.io/catthehacker/ubuntu:act-latest
act push -W .github/workflows/cd-frontend.yml -j build-and-deploy -P ubuntu-latest=ghcr.io/catthehacker/ubuntu:act-latest
act workflow_dispatch -W .github/workflows/cd-backend.yml -j deploy-infrastructure -P ubuntu-latest=ghcr.io/catthehacker/ubuntu:act-latest
```

Useful flags:

- `-W <workflow>` to target a single workflow
- `-j <job>` to run only the failing job
- `--eventpath <file>` to mirror GitHub event payloads
- `--secret-file <file>` for required secrets
- `--var <name>=<value>` when workflow vars are needed
- `-n` for a dry-run to inspect the execution plan
- `-v` or `--verbose` when the failure needs more trace detail
- `--container-architecture linux/amd64` when host architecture differences matter

On this repo, use the catthehacker Ubuntu image unless you have a strong reason to change it.

### 4. Build the reproduction environment intentionally

If the workflow needs secrets or vars:

- Create a local secret file such as `.act.secrets` and do not commit it.
- Use dummy values only when the failing step occurs before any real external call.
- If the failure is inside a deployment action, first reproduce all steps leading up to that action.

If the event payload matters, create a minimal JSON file that captures the fields the workflow actually reads. Do not fabricate a huge payload when only `ref`, `pull_request`, or changed paths matter.

### 5. Reproduce first, then debug

Once a local run fails, keep the loop tight:

1. Capture the exact failing command or action.
2. Decide whether the defect is in workflow YAML, repo scripts, dependencies, or local setup.
3. Fix the smallest root cause.
4. Rerun the same scoped `act` command.
5. Expand the validation only after the focused reproduction passes.

When a workflow step wraps a normal repo command such as `pnpm install`, `pnpm run build:all:prod`, or `dotnet test`, also run that command directly outside `act` when it helps isolate whether the problem is in the pipeline harness or in the application code.

### 6. Handle the common mismatch classes explicitly

Watch for these failure categories:

- Missing secrets or vars
- Wrong event assumptions, especially `push` versus `pull_request`
- Path filters preventing expected execution
- OS or shell differences
- Container image differences
- External services that `act` cannot fully emulate
- Action behavior that depends on GitHub-hosted features such as environments, OIDC, or deployment metadata

When `act` cannot fully reproduce a GitHub-hosted capability, still use it to validate the surrounding steps and narrow the remaining gap to the smallest external boundary.

## Repo Guidance By Workflow Type

### CI workflow

For broad CI failures, start with:

```powershell
pwsh ./scripts/act-ci.ps1
```

If that is too broad, switch to the failing workflow and job directly.

### Frontend CD workflow

For `.github/workflows/cd-frontend.yml`, expect these friction points:

- Cloudflare secrets and vars
- `wrangler-action` deployment behavior
- Post-deploy URL checks

Usually the right sequence is:

1. Reproduce `validate-secrets` or `build-and-deploy` with `act`.
2. Confirm `pnpm install` and `pnpm run build:all:prod` locally.
3. Validate generated files like `version.json` and `_redirects`.
4. Only then reason about the Cloudflare deployment step.

### Backend or infra CD workflows

These may depend on deployment credentials, environment protections, or infrastructure state that `act` cannot mirror perfectly. Reproduce the failing build, packaging, or shell logic locally first; treat the actual remote deployment boundary as the last thing to validate.

## Output Expectations

When using this skill, report back with:

1. The workflow, job, step, and event you targeted.
2. The exact local reproduction command.
3. The root cause you found.
4. The fix you applied, if any.
5. The verification you ran locally after the fix.
6. Any remaining differences between local `act` behavior and GitHub-hosted execution.

Keep the report concrete. The user should be able to rerun the same command and understand why the failure happened.

## Practical Rules

- Prefer the smallest reproducible job over the whole pipeline.
- Read the workflow YAML before running `act` blindly.
- Reuse repo scripts when they already encode the right baseline.
- Fix the root cause, not just the symptom observed in one run.
- Do not commit local secret files or event payloads unless the user explicitly wants durable fixtures.
- Stop only when the issue is fixed locally or the remaining blocker is clearly outside what `act` can simulate.