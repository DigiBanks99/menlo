#!pwsh
<#
.SYNOPSIS
    Run the CI workflow locally with act, as a pull_request from the current branch.

.DESCRIPTION
    Writes a pull_request event to .act/pull_request.json (gitignored), built from the
    merge base with origin/main, HEAD and the current branch, then runs act with it.
    Any extra arguments are passed to act, e.g. -W .github/workflows/ci.yml -j frontend-tests, or -n for a dry run.

.PARAMETER Sender
    The GitHub login that opened the pull request. Pass 'dependabot[bot]' to reproduce
    Dependabot-only behaviour: it sets the event's sender and act's --actor, because
    the workflows check github.actor.

.EXAMPLE
    pwsh ./scripts/act-ci.ps1
    pwsh ./scripts/act-ci.ps1 -Sender 'dependabot[bot]' -W .github/workflows/ci.yml -j backend-tests
#>
# No param block on purpose: PowerShell binds abbreviated parameter names, so a
# -Sender parameter would swallow act's -s (secret), and an advanced script would
# clash with -W and -v. -Sender is matched by its full name; everything else goes to act.
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$Sender = $null
$actArgs = [System.Collections.Generic.List[string]]::new()
for ($i = 0; $i -lt $args.Count; $i++) {
    if ($args[$i] -eq '-Sender') {
        if ($i + 1 -ge $args.Count) { throw '-Sender needs a value, e.g. -Sender dependabot[bot]' }
        $Sender = $args[++$i]
    }
    else {
        $actArgs.Add([string]$args[$i])
    }
}

Push-Location -Path (Join-Path $PSScriptRoot '..')
try {
    $baseSha = git merge-base origin/main HEAD
    if ($LASTEXITCODE -ne 0) { throw "git merge-base origin/main HEAD failed; run 'git fetch origin main' first." }
    $headSha = git rev-parse HEAD
    $headRef = git branch --show-current
    if (-not $headRef) { $headRef = $headSha }

    $pullRequestEvent = [ordered]@{
        pull_request = [ordered]@{
            base = [ordered]@{ sha = $baseSha; ref = 'main' }
            head = [ordered]@{ sha = $headSha; ref = $headRef }
        }
    }
    if ($Sender) { $pullRequestEvent.sender = @{ login = $Sender } }

    $eventDir = Join-Path (Get-Location) '.act'
    New-Item -ItemType Directory -Path $eventDir -Force | Out-Null
    $eventPath = Join-Path $eventDir 'pull_request.json'
    $pullRequestEvent | ConvertTo-Json -Depth 5 | Set-Content -Path $eventPath -Encoding utf8NoBOM

    $actorArgs = if ($Sender) { @('--actor', $Sender) } else { @() }

    act pull_request -e $eventPath -P ubuntu-latest=ghcr.io/catthehacker/ubuntu:act-latest @actorArgs @actArgs
    exit $LASTEXITCODE
}
finally {
    Pop-Location
}
