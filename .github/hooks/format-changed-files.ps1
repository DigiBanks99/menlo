#!/usr/bin/env pwsh

$ErrorActionPreference = "Stop"
$isVerbose = $env:VERBOSE -eq "1"

$editToolPattern = "(apply_patch|create_file|replace_string_in_file|multi_replace_string_in_file|edit_notebook_file|write|patch|edit)"

try {
    $stdinText = [Console]::In.ReadToEnd()
}
catch {
    $stdinText = ""
}

if (-not $stdinText) {
    if ($isVerbose) {
        Write-Host "Skipping: no hook payload on stdin."
    }
    exit 0
}

if ($stdinText -notmatch $editToolPattern) {
    if ($isVerbose) {
        Write-Host "Skipping: tool event was not an edit/write action."
    }
    exit 0
}

try {
    $files = @()
    $tracked = git diff --name-only
    if ($tracked) {
        $files += $tracked
    }

    $staged = git diff --name-only --cached
    if ($staged) {
        $files += $staged
    }

    $untracked = git ls-files --others --exclude-standard
    if ($untracked) {
        $files += $untracked
    }

    $files = $files | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Sort-Object -Unique
}
catch {
    Write-Host "Warning: unable to detect changed files. $_"
    exit 0
}

if (-not $files -or $files.Count -eq 0) {
    if ($isVerbose) {
        Write-Host "No changed files found to format."
    }
    exit 0
}

$netFiles = @()
$webFiles = @()

foreach ($file in $files) {
    $normalized = ($file -replace "\\", "/")
    if ($normalized -match "\.(cs|csproj|vb)$") {
        $netFiles += $normalized
    }
    elseif ($normalized -match "^src/ui/web/" -and $normalized -match "\.(ts|tsx|js|jsx|json)$") {
        $webFiles += $normalized
    }
}

$hasFailure = $false

if ($netFiles.Count -gt 0) {
    Write-Host "Formatting .NET files..."
    try {
        & dotnet format --include ($netFiles -join ",") --no-restore | Out-Null
        if ($LASTEXITCODE -ne 0) {
            $hasFailure = $true
            Write-Host "Warning: dotnet format exited with code $LASTEXITCODE"
        }
    }
    catch {
        $hasFailure = $true
        Write-Host "Warning: dotnet format failed. $_"
    }
}

if ($webFiles.Count -gt 0) {
    Write-Host "Formatting web files..."
    try {
        & pnpm format -- $webFiles | Out-Null
        if ($LASTEXITCODE -ne 0) {
            $hasFailure = $true
            Write-Host "Warning: pnpm format exited with code $LASTEXITCODE"
        }
    }
    catch {
        $hasFailure = $true
        Write-Host "Warning: pnpm format failed. $_"
    }
}

if ($hasFailure) {
    $blockPayload = @{
        decision = "block"
        stopReason = "Formatting failed for one or more changed files."
        systemMessage = "Formatting enforcement blocked this action. Fix formatting and retry."
    } | ConvertTo-Json -Compress
    Write-Output $blockPayload
    exit 2
}

exit 0
