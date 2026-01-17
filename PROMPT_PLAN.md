Study @docs/requirements/* to learn specifications (READ-ONLY, do not modify).
Study @AGENT.md for build/test commands.

Use up to 5 parallel subagents to study existing source code:
- src/api/ and src/lib/ for backend
- src/ui/web/projects/ for frontend

Compare implementation against specifications.

Create/update @docs/plans/fix_plan.md as a prioritized bullet list:
- Features not yet implemented (compare specs vs code)
- Search for: TODO, FIXME, NotImplementedException, placeholder
- Gaps between specifications and current implementation
- Failing tests or lint errors

Think hard. The plan must be actionable and prioritized by importance.
Do not modify any files in docs/requirements/ - they are read-only specifications.
