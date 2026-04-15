#Requires -Version 7.0
<#
.SYNOPSIS
    Ralph Wiggum Loop - Continuous Build
    
.DESCRIPTION
    Continuous build loop that runs GitHub Copilot with build prompts.
    Supports optional issue number parameter.
    
.PARAMETER Issue
    GitHub issue number to pass to the build prompt
    
.EXAMPLE
    .\ralph.ps1
    .\ralph.ps1 -Issue 123
#>

param(
    [string]$Issue = ""
)

$ErrorActionPreference = 'Stop'

Write-Host "Ralph Wiggum Loop - Continuous Build"
Write-Host "====================================="
Write-Host "Starting GitHub Copilot build loop..."
Write-Host "Press Ctrl+C to stop"
Write-Host ""

$loopCount = 0

while ($true) {
    $loopCount++
    Write-Host ""
    Write-Host "=== New Loop Iteration ==="
    Get-Date
    
    # Read the prompt from PROMPT_BUILD.md
    $prompt = Get-Content -Path "PROMPT_BUILD.md" -Raw
    
    # Replace $issueNumber with the actual issue number if provided
    if ($Issue) {
        $prompt = $prompt -replace '\$issueNumber', $Issue
    }
    
    # Run copilot with the prompt
    Write-Host "Running copilot..."
    copilot --yolo -p $prompt --autopilot
    
    Write-Host "Loop iteration complete. Sleeping 5s..."
    Start-Sleep -Seconds 5
}
