# Ralph Orchestration Loop

Autonomous AI-driven development loop using the Ralph Wiggum technique for building and evolving the Menlo application.

## Overview

This requirement establishes the infrastructure and workflow for running Claude Code in an autonomous loop that implements features, fixes bugs, and evolves the codebase based on specifications and a living plan.

## Key Documents

- [specifications.md](specifications.md) - Detailed technical specification
- [test-cases.md](test-cases.md) - Validation criteria
- [implementation.md](implementation.md) - Step-by-step implementation plan
- [diagrams/loop-flow.md](diagrams/loop-flow.md) - Visual flow diagram

## Quick Reference

| Artifact | Location | Purpose |
|----------|----------|---------|
| AGENT.md | `/AGENT.md` | Runtime context (self-improving) |
| PROMPT_PLAN.md | `/PROMPT_PLAN.md` | Planning loop instruction |
| PROMPT_BUILD.md | `/PROMPT_BUILD.md` | Building loop instruction |
| fix_plan.md | `/docs/plans/fix_plan.md` | Living TODO list |
| ralph.sh | `/ralph.sh` | Loop runner script |

## Dependencies

- Aspire CLI for validation
- Claude Code CLI with Pro subscription
- Podman for devcontainer sandboxing
- Git + GitHub CLI for PR creation
