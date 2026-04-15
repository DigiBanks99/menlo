#Requires -Version 7.0
<#
.SYNOPSIS
    Ralph Wiggum Loop - Continuous Build

.DESCRIPTION
    Continuous build loop that runs GitHub Copilot with build prompts.
    Supports optional issue number parameter. Stops when .ralph.done file is created.

.PARAMETER Issue
    GitHub issue number to pass to the build prompt

.EXAMPLE
    .\ralph.ps1
    .\ralph.ps1 -Issue 123
#>

param(
    [string]$Issue = "",
    [string]$Model = "claude-opus-4.5"
)

$ErrorActionPreference = 'Stop'

$doneFile = ".ralph.done"
$loopCount = 0

# Clean up any previous .ralph.done file at script start
if (Test-Path $doneFile) {
    Write-Host "Removing previous $doneFile..."
    Remove-Item $doneFile -Force
}

Write-Host "Ralph Wiggum Loop - Continuous Build"
Write-Host "====================================="
Write-Host "Starting GitHub Copilot build loop..."
Write-Host "Press Ctrl+C to stop (or loop will exit when $doneFile is created)"
Write-Host ""

while ($true) {
    $loopCount++
    Write-Host ""
    Write-Host "=== New Loop Iteration $loopCount ==="
    Get-Date

    # Read the prompt from PROMPT_BUILD.md
    $prompt = Get-Content -Path "PROMPT_BUILD.md" -Raw

    # Replace $issueNumber with the actual issue number if provided
    if ($Issue) {
        $prompt = $prompt -replace '\$issueNumber', $Issue
    }

    # Run copilot with the prompt
    Write-Host "Running copilot..."
    copilot --yolo -p $prompt --autopilot --model $Model

    # Check if completion file was created
    if (Test-Path $doneFile) {
        Write-Host ""
        Write-Host "=========================================="
        Write-Host "✓ Completion file detected: $doneFile"
        Write-Host "All acceptance criteria met. Exiting loop."
        Write-Host "=========================================="
        break
    }

    Write-Host "Loop iteration complete. Sleeping 5s..."
    Start-Sleep -Seconds 5
}

Write-Host "Ralph loop finished at $(Get-Date)"
