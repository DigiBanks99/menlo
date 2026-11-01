# Repo skills

Skills that only make sense for Menlo live here:

- `menlo-persistence`: EF Core and PostgreSQL persistence patterns
- `github-actions-act-debug`: reproducing GitHub Actions failures locally with `act`

General-purpose skills (aspire, playwright-cli, dotnet-inspect, commit-changes, dotnet-tests and others) are global and aren't copied into the repo.

Claude Code reads this folder. `.agents/skills` is a git symlink to it, so Copilot and other agents that read `.agents/skills` see the same skills, with nothing to keep in sync.

## Windows

Git checks a symlink out as a small text file unless both of these are true:

1. Developer Mode is on (Settings > System > For developers), or your account can create symlinks.
2. Symlinks are enabled for the repo: `git config core.symlinks true`

If `.agents/skills` is a text file containing `../.claude/skills`, turn both on and recreate it:

```shell
rm .agents/skills
git restore .agents/skills
```
