#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Parses one or more Cobertura coverage XML files and enforces per-assembly
    coverage thresholds. No test execution — this is the "fast" CI gate that
    runs *after* tests have already been executed under `dotnet-coverage`.

.DESCRIPTION
    Loaded thresholds come from a JSON file with the shape:

        {
          "globalThreshold": 60,
          "assemblies": { "Altinn.Foo": 75, ... },
          "warnings":   { "Altinn.Bar": 60, ... }
        }

    Only assemblies whose source files live under $OwnedRoot are enforced —
    that way a vertical's coverage run reports but does not *gate* on code
    owned by another vertical.

    Exit codes:
      0 — all owned assemblies meet their thresholds (or no thresholds set)
      1 — at least one owned assembly is below its threshold, or no coverage
          files matched the input pattern(s).

.PARAMETER CoverageFiles
    One or more paths (or glob patterns, resolved at invocation time) to
    Cobertura XML files produced by `dotnet-coverage collect` or similar.

.PARAMETER ThresholdsFile
    Path to the thresholds JSON file. Defaults to
    'coverage-thresholds.json' next to this script if omitted.

.PARAMETER Threshold
    Optional fallback global threshold (percent, integer) when the
    ThresholdsFile does not declare one.

.PARAMETER OwnedRoot
    Filesystem root whose assemblies are *enforced*. Assemblies whose
    source files fall outside this root are printed for reference only.
    Defaults to the current directory.
#>
param(
    [Parameter(Mandatory = $true)]
    [string[]]$CoverageFiles,
    [string]$ThresholdsFile,
    [int]$Threshold = 0,
    [string]$OwnedRoot = (Get-Location).Path
)
$ErrorActionPreference = 'Stop'

# Resolve any glob patterns passed in -CoverageFiles.
$resolved = @()
foreach ($p in $CoverageFiles) {
    $items = Get-ChildItem -Path $p -ErrorAction SilentlyContinue
    if ($items) { $resolved += $items.FullName }
}
if ($resolved.Count -eq 0) {
    Write-Host "No coverage files matched: $($CoverageFiles -join ', ')" -ForegroundColor Yellow
    exit 1
}
$CoverageFiles = $resolved | Sort-Object -Unique

Write-Host "=== Coverage Summary ===" -ForegroundColor Cyan
Write-Host "  Inputs:" -ForegroundColor DarkGray
foreach ($f in $CoverageFiles) { Write-Host "    $f" -ForegroundColor DarkGray }

# Load per-assembly thresholds.
$assemblyThresholds = @{}
$assemblyWarnings = @{}
$globalFloor = $Threshold
if (-not $ThresholdsFile) {
    $defaultPath = Join-Path $PSScriptRoot 'coverage-thresholds.json'
    if (Test-Path $defaultPath) { $ThresholdsFile = $defaultPath }
}
if ($ThresholdsFile -and (Test-Path $ThresholdsFile)) {
    $cfg = Get-Content $ThresholdsFile -Raw | ConvertFrom-Json
    if ($cfg.globalThreshold -and $globalFloor -eq 0) { $globalFloor = $cfg.globalThreshold }
    if ($cfg.assemblies) {
        $cfg.assemblies.PSObject.Properties | ForEach-Object { $assemblyThresholds[$_.Name] = [int]$_.Value }
    }
    if ($cfg.warnings) {
        $cfg.warnings.PSObject.Properties | ForEach-Object { $assemblyWarnings[$_.Name] = [int]$_.Value }
    }
    Write-Host "  Loaded thresholds from $ThresholdsFile" -ForegroundColor DarkGray
}

$ownedRootFull = try { (Resolve-Path $OwnedRoot -ErrorAction Stop).Path } catch { $OwnedRoot }
$ownedRootPrefix = $ownedRootFull.TrimEnd('\', '/')
Write-Host "  Enforcing thresholds only for assemblies owned by: $ownedRootPrefix" -ForegroundColor DarkGray

function Test-IsOwnedByVertical {
    param($pkg, [string]$rootPrefix)
    if (-not $rootPrefix) { return $true }
    $classes = $pkg.classes.class
    if (-not $classes) { return $false }
    foreach ($cls in $classes) {
        $fn = $cls.filename
        if (-not $fn) { continue }
        if ($fn.StartsWith($rootPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
            return $true
        }
    }
    return $false
}

$belowThreshold = @()
$warningsBelow = @()

# Phase 1 — aggregate per-assembly across all input cobertura files.
# An assembly touched by multiple test projects appears as one
# `<package>` entry per file; iterating naively (file × package) and
# applying the threshold to each occurrence would produce one
# false-positive failure per per-test-project view of an assembly
# whose canonical (union) coverage actually passes. CI's single-pass
# collection emits one merged cobertura, so this only matters for the
# workstation flow / direct invocations with multi-file input.
# `run-coverage.ps1` already calls `dotnet-coverage merge` upstream so
# this script normally gets a single input — the aggregation below is
# defense-in-depth (max line%/branch% is a sound conservative
# upper-bound when the inputs do not overlap perfectly).
$assemblies = @{}
foreach ($file in $CoverageFiles) {
    [xml]$xml = Get-Content $file
    foreach ($pkg in $xml.coverage.packages.package) {
        $n = $pkg.name
        if ($n -notmatch '^Altinn\.' -or $n -match 'Tests|TestUtils|Mocks') { continue }
        $lr = [math]::Round([double]$pkg.'line-rate' * 100, 2)
        $br = [math]::Round([double]$pkg.'branch-rate' * 100, 2)
        $owned = Test-IsOwnedByVertical -pkg $pkg -rootPrefix $ownedRootPrefix
        if (-not $assemblies.ContainsKey($n)) {
            $assemblies[$n] = [pscustomobject]@{ Line = $lr; Branch = $br; Owned = $owned }
        }
        else {
            $a = $assemblies[$n]
            if ($lr -gt $a.Line) { $a.Line = $lr }
            if ($br -gt $a.Branch) { $a.Branch = $br }
            # Owned status is the union: TRUE if any input cobertura
            # marks it owned. Same compiled assembly across files
            # should yield the same Owned verdict, but be defensive.
            if ($owned) { $a.Owned = $true }
        }
    }
}

# Phase 2 — print + threshold-check each assembly exactly once.
foreach ($n in ($assemblies.Keys | Sort-Object)) {
    $a = $assemblies[$n]
    $marker = if ($a.Owned) { '' } else { ' (ref)' }
    $c = if ($a.Line -lt 50) { 'Red' } elseif ($a.Line -lt 70) { 'Yellow' } else { 'Green' }
    Write-Host ("  {0,-50} {1,7}% line  {2,7}% branch{3}" -f $n, $a.Line, $a.Branch, $marker) -ForegroundColor $c
    if (-not $a.Owned) { continue }
    $floor = if ($assemblyThresholds.ContainsKey($n)) { $assemblyThresholds[$n] } else { $globalFloor }
    if ($floor -gt 0 -and $a.Line -lt $floor) {
        $belowThreshold += "$n ($($a.Line)% < $floor%)"
    }
    if ($assemblyWarnings.ContainsKey($n)) {
        $warnFloor = $assemblyWarnings[$n]
        if ($warnFloor -gt 0 -and $a.Line -lt $warnFloor) {
            $warningsBelow += "$n ($($a.Line)% < $warnFloor%)"
        }
    }
}

if ($warningsBelow.Count -gt 0) {
    Write-Host "`nWarning - below ratchet (non-fatal):" -ForegroundColor Yellow
    $warningsBelow | ForEach-Object { Write-Host "  $_" -ForegroundColor Yellow }
}

if ($belowThreshold.Count -gt 0) {
    Write-Host "`nBelow threshold:" -ForegroundColor Red
    $belowThreshold | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
    exit 1
}
elseif ($globalFloor -gt 0 -or $assemblyThresholds.Count -gt 0) {
    Write-Host "`nAll enforced assemblies meet their coverage thresholds." -ForegroundColor Green
}
