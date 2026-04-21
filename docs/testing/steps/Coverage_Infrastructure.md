# Coverage Infrastructure

## Goal

Set up code coverage collection and reporting infrastructure that works with
xUnit v3 self-hosted test projects.

## Key Finding: coverlet vs xUnit v3

`coverlet.collector` (the data-collector approach used with `dotnet test --collect`)
does **not** work with xUnit v3 self-hosted executables (`OutputType=Exe`).
The vstest platform never controls the test process, so the profiler cannot attach.

**Solution:** Use `dotnet-coverage` (Microsoft's dynamic instrumentation tool)
to wrap the test exe directly:

```
dotnet-coverage collect --output results.cobertura.xml --output-format cobertura ./MyTests.exe
```

This bypasses vstest entirely and instruments at the CLR level.

## Baseline Coverage (Authorization.Tests)

| Assembly | Line% | Branch% |
|---|---|---|
| Altinn.Authorization | 62.77 | 63.50 |
| Altinn.Authorization.ABAC | 62.91 | 61.29 |
| Altinn.Authorization.PEP | 7.68 | 13.60 |

## Changes

### `src/Directory.Build.targets`
- Added `CoverletOutputFormat=cobertura` property for test projects
- Added `coverlet.msbuild` package reference alongside `coverlet.collector`

### `src/coverage.runsettings`
- New runsettings file with Cobertura format, attribute exclusions, and
  assembly include/exclude filters

### `src/Directory.Packages.props` (all 3)
- Added `coverlet.msbuild` version entry to all Directory.Packages.props files

### `docs/testing/run-coverage.ps1`
- Coverage collection script using `dotnet-coverage`
- Auto-discovers test projects, detects xUnit v3 exes, falls back to vstest
- Prints color-coded summary table
- Supports `-Threshold` parameter for CI enforcement
- Supports optional ReportGenerator HTML output

### `.gitignore`
- Added `TestResults/` exclusion

## Usage

```powershell
# Run all test projects with coverage
pwsh docs/testing/run-coverage.ps1

# With 50% minimum threshold (exits 1 if below)
pwsh docs/testing/run-coverage.ps1 -Threshold 50

# Single project, skip build
pwsh docs/testing/run-coverage.ps1 -Projects @("path/to/Tests.csproj") -NoBuild
```

## Prerequisites

- `dotnet-coverage` global tool: `dotnet tool install -g dotnet-coverage`
- Optional: `dotnet tool install -g dotnet-reportgenerator-globaltool`

## Verification

- Build: successful
- Script tested against Authorization.Tests: 210/210 passed, coverage data collected
