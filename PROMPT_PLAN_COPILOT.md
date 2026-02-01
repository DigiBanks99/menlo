# Planning Prompt for GitHub Copilot CLI

Study the specifications in `/workspaces/menlo/docs/requirements/*` to learn project requirements (READ-ONLY, do not modify).
Study `/workspaces/menlo/AGENT.md` for build/test commands.

Analyze the existing source code structure:
- `/workspaces/menlo/src/api/` and `/workspaces/menlo/src/lib/` for backend implementation
- `/workspaces/menlo/src/ui/web/projects/` for frontend implementation

Compare the current implementation against the specifications in `docs/requirements/`.

Create or update `/workspaces/menlo/docs/plans/fix_plan.md` as a prioritized bullet list containing:
- Features defined in specifications but not yet implemented (compare specs vs actual code)
- Search codebase for markers indicating incomplete work: `TODO`, `FIXME`, `NotImplementedException`, placeholder code
- Gaps between what specifications require and what currently exists
- Failing tests or lint errors reported by the build system

Think critically and thoroughly. The plan must be:
- Actionable with clear next steps
- Prioritized by importance (blockers first, then features, then improvements)
- Based on evidence from both specifications and code

**Important:** Do not modify any files in `docs/requirements/` - they are read-only specifications that define the project scope and requirements.
