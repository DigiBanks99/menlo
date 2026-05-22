#!pwsh
<#
.SYNOPSIS
    Checks that changed C# files under src/api/Menlo.Api/ meet a minimum line coverage threshold.

.DESCRIPTION
    Parses Cobertura XML coverage files, aggregates unique source line numbers per file across all
    matching class nodes (including async/display classes), and fails if any changed Menlo.Api file
    is missing from the report or falls below the threshold.

.PARAMETER CoverageDir
    Directory containing coverage.cobertura.xml file(s) (searched recursively). Default: coverage/api-baseline

.PARAMETER Threshold
    Minimum line coverage percentage (0-100). Default: 70

.PARAMETER BaseSha
    Base git commit SHA for diff. If empty, auto-detected from environment.

.PARAMETER HeadSha
    Head git commit SHA for diff. If empty, defaults to HEAD.
#>

[CmdletBinding()]
param(
    [string]$CoverageDir = "coverage/api-baseline",
    [int]$Threshold = 70,
    [string]$BaseSha = "",
    [string]$HeadSha = "HEAD"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# ── Helper: normalize a path to forward slashes and lowercase for comparison ──
function Normalize-Path ([string]$p) {
    return $p.Replace('\', '/').ToLowerInvariant()
}

# ── 1. Determine base SHA for diff ───────────────────────────────────────────
if (-not $BaseSha) {
    if ($env:GITHUB_EVENT_NAME -eq 'pull_request') {
        $BaseSha = $env:GITHUB_BASE_SHA
        if (-not $BaseSha) {
            $BaseSha = "origin/$($env:GITHUB_BASE_REF ?? 'main')"
        }
    } elseif ($env:GITHUB_EVENT_BEFORE -and $env:GITHUB_EVENT_BEFORE -ne '0000000000000000000000000000000000000000') {
        $BaseSha = $env:GITHUB_EVENT_BEFORE
    } else {
        $BaseSha = "HEAD~1"
    }
}

Write-Host "Using base SHA: $BaseSha  head SHA: $HeadSha"

# ── 2. Get changed C# files under src/api/Menlo.Api/ ────────────────────────
$gitArgs = @('diff', '--name-only', $BaseSha, $HeadSha, '--', 'src/api/Menlo.Api/*.cs', 'src/api/Menlo.Api/**/*.cs')
$changedRaw = & git @gitArgs 2>&1

if ($LASTEXITCODE -ne 0) {
    Write-Warning "git diff failed (exit $LASTEXITCODE). Falling back to HEAD~1."
    $gitArgs[2] = 'HEAD~1'
    $changedRaw = & git @gitArgs 2>&1
}

# Normalize changed file paths to forward slashes while retaining original casing for file reads.
$changedFiles = @($changedRaw |
    Where-Object { $_ -match '\.cs$' } |
    ForEach-Object {
        [pscustomobject]@{
            Original   = $_
            Normalized = Normalize-Path $_
        }
    })

if ($changedFiles.Count -eq 0) {
    Write-Host "✅ No changed C# files in src/api/Menlo.Api/ — skipping coverage check."
    exit 0
}

Write-Host "Changed Menlo.Api files ($($changedFiles.Count)):"
$changedFiles | ForEach-Object { Write-Host "  $($_.Normalized)" }

# ── 3. Find coverage XML files ───────────────────────────────────────────────
$xmlFiles = @(Get-ChildItem -Path $CoverageDir -Recurse -Filter 'coverage.cobertura.xml' -ErrorAction SilentlyContinue)

if ($xmlFiles.Count -eq 0) {
    Write-Error "❌ No coverage.cobertura.xml found under '$CoverageDir'. Run tests with --collect:'XPlat Code Coverage' first."
    exit 1
}

Write-Host "Found $($xmlFiles.Count) coverage XML file(s)."

# ── 4. Parse XML: aggregate lines per source file ───────────────────────────
# Key: normalized filename relative to the <source> root (e.g. "api/menlo.api/budget/budgetdto.cs")
# Value: hashtable { covered: set<int>, total: set<int> }
$fileCoverage = @{}

foreach ($xmlFile in $xmlFiles) {
    [xml]$xml = Get-Content $xmlFile.FullName -Raw

    foreach ($class in $xml.SelectNodes('//class')) {
        $rawFilename = $class.GetAttribute('filename')
        if (-not $rawFilename) { continue }

        # Skip generated obj/ files
        if ($rawFilename -match '[/\\]obj[/\\]') { continue }

        $normFile = Normalize-Path $rawFilename

        # Only process files belonging to src/api/Menlo.Api/
        # XML stores path relative to <source>, so it looks like "api/menlo.api/..."
        if ($normFile -notmatch '^api/menlo\.api/') { continue }

        if (-not $fileCoverage.ContainsKey($normFile)) {
            $fileCoverage[$normFile] = @{ Covered = [System.Collections.Generic.HashSet[int]]::new()
                                          Total   = [System.Collections.Generic.HashSet[int]]::new() }
        }

        foreach ($line in $class.SelectNodes('lines/line')) {
            $num  = [int]$line.GetAttribute('number')
            $hits = [int]$line.GetAttribute('hits')
            [void]$fileCoverage[$normFile].Total.Add($num)
            if ($hits -gt 0) {
                [void]$fileCoverage[$normFile].Covered.Add($num)
            }
        }
    }
}

# ── 5. Check each changed file ───────────────────────────────────────────────
$failed  = $false
$results = @()

foreach ($changedFile in $changedFiles) {
    # changed file: "src/api/menlo.api/budget/budgetdto.cs"
    # xml key:      "api/menlo.api/budget/budgetdto.cs"
    $xmlKey = $changedFile.Normalized -replace '^src/', ''

    if (-not $fileCoverage.ContainsKey($xmlKey)) {
        $isExcludedFromCoverage = $false
        if (Test-Path -LiteralPath $changedFile.Original) {
            $fileContent = Get-Content -LiteralPath $changedFile.Original -Raw
            $isExcludedFromCoverage = $fileContent -match '\[(?:\s*System\.Diagnostics\.CodeAnalysis\.)?ExcludeFromCodeCoverage(?:Attribute)?(?:\s*\(.*?\))?\s*\]'
        }

        if ($isExcludedFromCoverage) {
            Write-Host "ℹ️  $($changedFile.Normalized) — excluded from code coverage via [ExcludeFromCodeCoverage], skipping."
            $results += [pscustomobject]@{ File = $changedFile.Normalized; Coverage = 'N/A'; Status = 'EXCLUDED' }
            continue
        }

        Write-Warning "⚠️  $($changedFile.Normalized) — not found in coverage report"
        $results += [pscustomobject]@{ File = $changedFile.Normalized; Coverage = 'N/A'; Status = 'MISSING' }
        $failed = $true
        continue
    }

    $info     = $fileCoverage[$xmlKey]
    $total    = $info.Total.Count
    $covered  = $info.Covered.Count

    if ($total -eq 0) {
        $pct = 100
    } else {
        $pct = [math]::Round(($covered / $total) * 100, 1)
    }

    $status = if ($pct -ge $Threshold) { 'PASS' } else { 'FAIL'; $failed = $true }
    $results += [pscustomobject]@{ File = $changedFile.Normalized; Coverage = "$pct%"; Status = $status }
}

# ── 6. Print summary ─────────────────────────────────────────────────────────
Write-Host "`nAPI Coverage Baseline Check (threshold: $Threshold%)"
Write-Host ('-' * 80)
$results | Format-Table -AutoSize | Out-String | Write-Host

if ($failed) {
    Write-Error "❌ Coverage baseline check FAILED. One or more changed files are below ${Threshold}% line coverage."
    exit 1
} else {
    Write-Host "✅ Coverage baseline check PASSED. All changed files meet ${Threshold}% line coverage."
    exit 0
}
