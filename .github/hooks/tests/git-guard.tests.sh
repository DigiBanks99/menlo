#!/usr/bin/env bash
# Black-box tests for .github/hooks/git-guard.cs: pipe hook payloads in, check the exit code.
# Builds a throwaway repo so branch state is predictable. Run: bash .github/hooks/tests/git-guard.tests.sh
set -u

script_dir="$(cd "$(dirname "$0")" && pwd)"
guard="$script_dir/../git-guard.cs"

work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT
# Hook payloads carry native paths; on Git for Windows, /tmp/... means nothing to .NET.
if command -v cygpath >/dev/null 2>&1; then
    work="$(cygpath -m "$work")"
fi

git -C "$work" init -q -b main
git -C "$work" config user.email test@example.com
git -C "$work" config user.name test
echo hi > "$work/file.txt"
git -C "$work" add file.txt
git -C "$work" commit -q -m init
git -C "$work" branch feature

# Build once so the per-case timing is the cached run.
dotnet build "$guard" -v q -nologo >/dev/null || { echo "build failed"; exit 1; }

failures=0
passes=0

json_escape() {
    printf '%s' "$1" | sed -e 's/\\/\\\\/g' -e 's/"/\\"/g'
}

# expect <exit-code> <tool-name> <command> [branch]
expect() {
    local expected="$1" tool="$2" cmd="$3" branch="${4:-main}"
    # Visit main first, so @{-1} is main whenever the case starts on another branch.
    git -C "$work" checkout -q main
    git -C "$work" checkout -q "$branch"
    local payload
    payload="{\"tool_name\":\"$tool\",\"cwd\":\"$(json_escape "$work")\",\"tool_input\":{\"command\":\"$(json_escape "$cmd")\"}}"
    local stderr actual
    stderr="$(printf '%s' "$payload" | dotnet run --no-build --file "$guard" 2>&1 >/dev/null)"
    actual=$?
    if [ "$actual" -eq "$expected" ]; then
        passes=$((passes + 1))
    else
        failures=$((failures + 1))
        echo "FAIL [$branch] $tool: $cmd -> exit $actual, expected $expected ${stderr:+($stderr)}"
    fi
}

# Commits on main are blocked
expect 2 Bash 'git commit -m "x"'
expect 2 Bash 'git add . && git commit -m x'
expect 2 Bash 'git -C . commit --amend --no-edit'
expect 2 PowerShell 'git add .; git commit -m x'
expect 2 run_in_terminal 'git cherry-pick abc123'
expect 2 Bash 'git switch main && git commit -m x' feature

expect 2 Bash 'env GIT_EDITOR=true git commit -m x'
expect 2 Bash "bash -c 'git commit -m x'"
expect 2 PowerShell "pwsh -Command 'git commit -m x'"
expect 2 Bash 'git checkout - && git commit -m x' feature
expect 2 Bash 'git switch - && git commit -m x' feature
expect 2 Bash 'git switch feature && git switch - && git commit -m x'
expect 2 PowerShell "git -C $(printf '%s' "$work" | tr '/' '\\') commit -m x"
if command -v cygpath >/dev/null 2>&1; then
    expect 2 Bash "git -C $(cygpath -u "$work") commit -m x"
fi

# Commits elsewhere, or after leaving main in the same command, are allowed
expect 0 Bash 'git commit -m x' feature
expect 0 Bash 'git switch -c feat/1-x && git commit -m x'
expect 0 Bash 'git checkout -b feat/1-x && git commit -m x'
expect 0 Bash 'git commit --help'
expect 0 Bash 'git switch --detach main && git commit -m x'
expect 0 Bash 'git checkout --detach main && git commit -m x'
expect 0 Bash 'echo "git commit on main is blocked"'
expect 0 Bash 'git log --oneline -5'
expect 0 Bash 'git status'

# Branch navigation is allowed
expect 0 Bash 'git checkout main' feature
expect 0 Bash 'git checkout feature'
expect 0 Bash 'git checkout -b feat/2-y'
expect 0 Bash 'git checkout -b feat/2-y main'
expect 0 Bash 'git checkout -'
expect 0 Bash 'git checkout --detach HEAD'
expect 0 Bash 'git switch feature'
expect 0 Bash 'git switch -c feat/3-z'
expect 0 Bash 'git switch --create feat/3-z main'
expect 0 Bash 'git switch main' feature

# Forms that discard work are blocked
expect 2 Bash 'git checkout -- file.txt'
expect 2 Bash 'git checkout .'
expect 2 Bash 'git checkout file.txt'
expect 2 Bash 'git checkout main -- file.txt'
expect 2 Bash 'git checkout main file.txt'
expect 2 Bash 'git checkout -f feature'
expect 2 Bash 'git checkout --force feature'
expect 2 Bash 'git checkout --theirs file.txt'
expect 2 Bash 'git checkout -p'
expect 2 Bash 'git checkout -B feature'
expect 2 Bash 'git switch --discard-changes feature'
expect 2 Bash 'git switch -f feature'
expect 2 Bash 'git switch -C feature'
expect 2 PowerShell 'cd src; git checkout .'

# Input that is not a shell command is ignored
expect 0 Edit ''
printf 'not json' | dotnet run --no-build --file "$guard" >/dev/null 2>&1
if [ $? -eq 0 ]; then passes=$((passes + 1)); else failures=$((failures + 1)); echo "FAIL invalid JSON should exit 0"; fi

echo "$passes passed, $failures failed"
[ "$failures" -eq 0 ]
