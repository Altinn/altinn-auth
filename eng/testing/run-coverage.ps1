#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Local-dev helper: builds and runs every *.Tests project in the repo under
    `dotnet-coverage collect`, producing a per-project Cobertura XML, then
    delegates threshold enforcement to check-coverage-thresholds.ps1.

.DESCRIPTION
    CI does NOT use this script. The CI pipeline runs tests once under
    `dotnet-coverage collect 'dotnet test'` in the workflow itself and then
    invokes check-coverage-thresholds.ps1 directly on the resulting
    cobertura file. This script exists for developer use on a workstation.
#>
param(
    [int]$Threshold = 0,
    [string[]]$Projects,
    [string]$Configuration = 'Release',
    [switch]$NoBuild,
    [string]$ThresholdsFile,
    [string]$OwnedRoot = (Get-Location).Path
)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$srcRoot = Join-Path $repoRoot 'src'
$resultsDir = Join-Path $repoRoot 'TestResults'

$dotnetCov = Get-Command dotnet-coverage -ErrorAction SilentlyContinue
if (-not $dotnetCov) {
    Write-Host "Installing dotnet-coverage..." -ForegroundColor Yellow
    dotnet tool install -g dotnet-coverage
}

if (Test-Path $resultsDir) { Remove-Item $resultsDir -Recurse -Force }
New-Item -ItemType Directory -Path $resultsDir -Force | Out-Null

if (-not $Projects) {
    $Projects = Get-ChildItem -Path $srcRoot -Filter '*.Tests.csproj' -Recurse |
        Select-Object -ExpandProperty FullName
}
Write-Host "Found $($Projects.Count) test project(s)" -ForegroundColor Cyan

if (-not $NoBuild) {
    foreach ($proj in $Projects) {
        dotnet build $proj --configuration $Configuration --verbosity minimal
        if ($LASTEXITCODE -ne 0) { exit 1 }
    }
}

$failed = @()
$coverageFiles = @()

# Run per-project coverage collection in parallel so the dominant project
# (e.g. AccessMgmt.Tests at ~2m) sets the wall-clock, instead of the sum
# of all project times. Each project writes to a distinct output + log.
$configurationLocal = $Configuration
$resultsDirLocal = $resultsDir
$throttle = [Math]::Min($Projects.Count, [Math]::Max(2, [Environment]::ProcessorCount))

$collectResults = $Projects | ForEach-Object -Parallel {
    $proj = $_
    $projName = [System.IO.Path]::GetFileNameWithoutExtension($proj)
    $projDir = [System.IO.Path]::GetDirectoryName($proj)

    $binDir = Join-Path $projDir "bin" $using:configurationLocal "net9.0"
    $dllPath = Join-Path $binDir "$projName.dll"
    $runtimeConfig = Join-Path $binDir "$projName.runtimeconfig.json"
    $outFile = Join-Path $using:resultsDirLocal "$projName.cobertura.xml"
    $logFile = Join-Path $using:resultsDirLocal "$projName.coverage.log"

    # xUnit v3 test projects build as executables (OutputType=Exe) and run on
    # Microsoft Testing Platform. A runtimeconfig.json is always emitted for
    # Exe output; running the managed dll via `dotnet <dll>` works
    # cross-platform (Windows, Linux, macOS) under dotnet-coverage.
    $isV3Exe = (Test-Path $dllPath) -and (Test-Path $runtimeConfig)

    $start = Get-Date
    if ($isV3Exe) {
        # Invoking the managed dll directly uses xUnit v3's native in-process
        # runner (not MTP). It exits 0 when every test is [Skip]ped, so no
        # --ignore-exit-code handling is required. Test stdout is captured to
        # a per-project log to keep the coverage summary readable.
        dotnet-coverage collect --nologo --output $outFile --output-format cobertura -- dotnet $dllPath *>&1 | Out-File -FilePath $logFile -Encoding utf8
        $mode = 'xUnit v3 / MTP'
    }
    else {
        $tmpDir = Join-Path $using:resultsDirLocal $projName
        dotnet test $proj --no-build --configuration $using:configurationLocal --collect:"XPlat Code Coverage" --results-directory $tmpDir --logger "console;verbosity=minimal" *>&1 | Out-File -FilePath $logFile -Encoding utf8
        $cobFile = Get-ChildItem -Path $tmpDir -Filter 'coverage.cobertura.xml' -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($cobFile) { Copy-Item $cobFile.FullName $outFile }
        $mode = 'vstest fallback'
    }

    [pscustomobject]@{
        Name      = $projName
        Mode      = $mode
        Exit      = $LASTEXITCODE
        OutFile   = $outFile
        LogFile   = $logFile
        DurationS = [int]((Get-Date) - $start).TotalSeconds
    }
} -ThrottleLimit $throttle

foreach ($r in $collectResults) {
    Write-Host "`n=== $($r.Name) ===" -ForegroundColor Green
    Write-Host "  dotnet-coverage collect ($($r.Mode)) in $($r.DurationS)s -> log: $($r.LogFile)" -ForegroundColor DarkGray
    if ($r.Exit -ne 0) {
        $failed += $r.Name
        Write-Host "  FAILED (exit $($r.Exit)) - tail of $($r.LogFile) :" -ForegroundColor Red
        if (Test-Path $r.LogFile) { Get-Content $r.LogFile -Tail 80 | ForEach-Object { Write-Host "    $_" } }
    }
    if (Test-Path $r.OutFile) { $coverageFiles += $r.OutFile }
}

if ($coverageFiles.Count -eq 0) {
    Write-Host "No coverage files generated." -ForegroundColor Yellow
    exit 1
}

# Merge per-project cobertura files into a single aggregate before
# threshold checking. Without this, an assembly touched by multiple
# test projects (e.g. AccessManagement.Core covered both directly by
# AccessMgmt.Tests and transitively by Enduser.Api.Tests) shows up
# as multiple separate package entries — the threshold check then
# trips on whichever per-project view is below the floor, even when
# the union coverage passes. CI does not hit this because it runs a
# single-pass `dotnet-coverage collect -- dotnet test` that emits one
# merged cobertura by construction.
$mergedCoverage = Join-Path $resultsDir 'coverage.cobertura.xml'
Write-Host "`nMerging $($coverageFiles.Count) per-project cobertura files into $mergedCoverage ..." -ForegroundColor DarkGray
dotnet-coverage merge --output $mergedCoverage --output-format cobertura $coverageFiles
if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to merge cobertura files (dotnet-coverage merge exit $LASTEXITCODE)." -ForegroundColor Red
    exit 1
}

# Delegate threshold enforcement + pretty summary to the parse-only script
# (shared with CI, so there's no drift between local and CI output).
# Pass only the merged file so each assembly appears exactly once.
$checkScript = Join-Path $PSScriptRoot 'check-coverage-thresholds.ps1'
$checkArgs = @{
    CoverageFiles = @($mergedCoverage)
    OwnedRoot     = $OwnedRoot
}
if ($ThresholdsFile) { $checkArgs['ThresholdsFile'] = $ThresholdsFile }
if ($Threshold -gt 0) { $checkArgs['Threshold'] = $Threshold }
& $checkScript @checkArgs
$thresholdExit = $LASTEXITCODE

$rg = Get-Command reportgenerator -ErrorAction SilentlyContinue
if ($rg) {
    $rd = Join-Path $resultsDir 'Report'
    reportgenerator "-reports:$mergedCoverage" "-targetdir:$rd" "-reporttypes:Html;TextSummary"
    Write-Host "`nReport: $rd\index.html" -ForegroundColor Cyan
}
else {
    Write-Host "`nTip: dotnet tool install -g dotnet-reportgenerator-globaltool" -ForegroundColor Yellow
}

if ($failed.Count -gt 0) {
    Write-Host "`nTest failures: $($failed -join ', ')" -ForegroundColor Red
    exit 1
}
if ($thresholdExit -ne 0) {
    exit $thresholdExit
}
Write-Host "`nDone." -ForegroundColor Green
