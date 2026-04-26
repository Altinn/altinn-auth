# Code Coverage

The repo uses `dotnet-coverage` to produce Cobertura reports and a small
PowerShell wrapper to parse them and enforce per-assembly thresholds. CI fails
the build if any **enforced** assembly drops below its floor.

## Files

| File | What it is |
|---|---|
| [`eng/testing/coverage-thresholds.json`](../../eng/testing/coverage-thresholds.json) | Source of truth for per-assembly minimum line coverage |
| [`eng/testing/check-coverage-thresholds.ps1`](../../eng/testing/check-coverage-thresholds.ps1) | Parse-only script (reads a Cobertura file, pretty-prints a table, enforces thresholds, exits non-zero on failure) |
| [`eng/testing/run-coverage.ps1`](../../eng/testing/run-coverage.ps1) | **Local-dev** helper: builds every `*.Tests` project, runs each under `dotnet-coverage collect` in parallel, then invokes the check script |
| [`eng/testing/run-accessmanagement-coverage.ps1`](../../eng/testing/run-accessmanagement-coverage.ps1) | Local-dev helper scoped to the AccessManagement vertical (faster feedback) |

## Thresholds

See [`coverage-thresholds.json`](../../eng/testing/coverage-thresholds.json).
Two sections:

### `assemblies` — **enforced** (CI-breaking)

```json
{
  "Altinn.Authorization": 60,
  "Altinn.Authorization.ABAC": 60,
  "Altinn.Authorization.PEP": 75,
  "Altinn.AccessMgmt.PersistenceEF": 90,
  "Altinn.AccessManagement.Api.Maskinporten": 75,
  "Altinn.AccessManagement.Api.Enterprise": 60,
  "Altinn.AccessManagement.Core": 60
}
```

Dropping below the floor for any of these fails the pipeline.

### `warnings` — ratcheting (non-fatal)

```json
{
  "Altinn.AccessManagement": 60
}
```

Below-threshold assemblies here emit a warning in the CI log. Use this section
as a one-way ratchet for assemblies approaching promotion to `assemblies`.

### `globalThreshold`

Currently `0` — only the explicitly listed assemblies are gated. We
deliberately do **not** enforce coverage on every assembly in the repo;
many are thin adapters, generated code, or host wiring where line coverage
isn't a meaningful target.

## Running locally

Full repo:

```
pwsh eng/testing/run-coverage.ps1
```

Just AccessManagement:

```
pwsh eng/testing/run-accessmanagement-coverage.ps1
```

Scope to specific projects:

```
pwsh eng/testing/run-coverage.ps1 -Projects @(
    "src/apps/Altinn.AccessManagement/test/Altinn.AccessManagement.Api.Tests/Altinn.AccessManagement.Api.Tests.csproj"
)
```

Outputs land in `TestResults/`:

- `*.cobertura.xml` — one per test project
- `*.coverage.log` — captured test stdout (last N lines are echoed on failure)

## How CI runs it (single pass)

CI does **not** run `run-coverage.ps1`. Instead, it runs the test suite once
under `dotnet-coverage collect` and then invokes the parse-only check:

```yaml
# Step A — run the tests once, producing TRX + coverage in one pass
dotnet-coverage collect --output TestResults/coverage.cobertura.xml \
    --output-format cobertura -- \
    dotnet test -- --report-xunit-trx --ignore-exit-code 8

# Step B — parse-only threshold enforcement (seconds)
pwsh eng/testing/check-coverage-thresholds.ps1 \
    -CoverageFile TestResults/coverage.cobertura.xml \
    -ThresholdsFile eng/testing/coverage-thresholds.json
```

Design rationale: see
[`TESTING_INFRASTRUCTURE_OVERHAUL/STEPS_PART_1/41_CI_Coverage_Single_Run.md`](TESTING_INFRASTRUCTURE_OVERHAUL/STEPS_PART_1/41_CI_Coverage_Single_Run.md).
Before Step 41, tests ran twice (once for Test, once for Coverage), which
doubled pipeline time.

## Adjusting a threshold

- **Raising** an existing threshold: open a PR that bumps the number in
  `coverage-thresholds.json`. No other change needed.
- **Adding** a new enforced assembly: add it to `assemblies` with a
  realistic floor. If current coverage is close but not quite there, add
  it to `warnings` first and promote later.
- **Lowering** a threshold: discouraged. Include a short rationale in the PR
  description (e.g. "deleted an entire well-covered module"). Prefer adding
  tests.

## Current coverage

See the *Final Coverage (measured)* table at the bottom of
[`TESTING_INFRASTRUCTURE_OVERHAUL/STEPS_PART_1/INDEX.md`](TESTING_INFRASTRUCTURE_OVERHAUL/STEPS_PART_1/INDEX.md) for the latest measured numbers per
assembly.

## Next: [CI.md](CI.md)
