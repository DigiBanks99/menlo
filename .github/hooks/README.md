# Hooks

Which hook does what for each agent, and for git. Claude Code and Copilot read different config formats, so each has its own config file, but every config calls the same scripts and holds no logic of its own (decision on #485).

| What | Script | Git | Claude Code | Copilot (VS Code) |
| --- | --- | --- | --- | --- |
| Format staged files before a commit | [`.githooks/pre-commit`](../../.githooks/pre-commit) | `pre-commit` hook | runs through git | runs through git |
| Block commits on `main` and `git checkout`/`git switch` forms that discard work | [`git-guard.cs`](git-guard.cs) | n/a | `PreToolUse` in [`.claude/settings.json`](../../.claude/settings.json) (Bash and PowerShell; a shell pre-filter skips payloads without `git`) | `PreToolUse` in [`git-guard.json`](git-guard.json) |

`.claude/settings.json` also holds the permission allowlist for `dotnet`, `pnpm`, `nx`, `aspire`, `gh`, read-only git and branch navigation. Its `ask` list keeps a prompt on destructive `gh` commands: issue delete and transfer, repo and release deletes, secrets, variables, rulesets, PR merges, and `gh api` DELETE/PUT.

## Format on commit

`pnpm install` at the repo root runs `git config core.hooksPath .githooks`, unless you have already set a `core.hooksPath`. Then it leaves yours alone, and you'll need to call `.githooks/pre-commit` from your own hook. To enable it by hand, run `git config core.hooksPath .githooks`.

The hook formats the staged `.cs`/`.csproj` files with `dotnet format`, and the staged `src/ui/web` files with `nx format:write`. It then re-stages them. If a file is only partly staged, the hook stops: re-staging it would pull in the unstaged hunks. Skip the hook once with `git commit --no-verify`.

## Git guard

`git-guard.cs` is a .NET file-based app, run with `dotnet run --file`. The first run builds it, which takes about 20 s. After that it is cached and takes about 0.5 s. It reads the hook payload on stdin and checks `tool_input.command`:

- `commit`, `cherry-pick`, `revert` and `am` are blocked while the branch is `main` (or `master`). Switching to `main` is fine. `git switch -c x && git commit` is judged on `x`.
- `git checkout` is allowed only for branch navigation: `<branch>`, `-b <new> [<start>]`, `-`, `--detach`. Paths, `--`, `-f`, `--ours/--theirs`, `-p`, `-m` and `-B` are blocked.
- `git switch` is blocked with `--discard-changes`, `-f`, `-m` or `-C`.
- It looks through `env`, `sudo`, `time`, `xargs` and similar wrappers, and into `sh -c`, `bash -c`, `pwsh -Command` and `cmd /c` strings. It sees the command text only, so `$VAR` and `$(...)` are not expanded.

A block exits 2 with the reason on stderr; both agents show that to the model. Anything else exits 0 with no output. In Claude Code the hook command fails closed: if the guard can't run at all (dotnet missing, or a build error or lock), it exits 2 and git commands are blocked until it runs again. The Copilot entry has no tool filter, so the guard starts for every Copilot tool call. That costs about 0.5 s, the same order as the old format hook.

The code avoids reflection-based serialization, so it stays AOT-safe. To use a native binary, `dotnet publish .github/hooks/git-guard.cs` (this needs the platform C++ linker) and point both configs at the output.

Test it with `bash .github/hooks/tests/git-guard.tests.sh`.

## Revisit

Keeping both configs makes switching agents easy, at the cost of upkeep. Merge them if Claude Code adopts a shared hook format, or drop `git-guard.json` if Copilot is no longer used.
