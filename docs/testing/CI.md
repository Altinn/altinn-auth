# Tests in CI

Tests run per-vertical in parallel jobs. Each vertical (app/lib/pkg) has its
own CI job driven by a shared template: `tpl-vertical-ci.yml`. Build, test,
threshold enforcement, and SonarCloud analysis all happen in one job per
vertical (`build-test-analyze`) so the test suite executes exactly once.

## Job structure

For each vertical the pipeline runs:

1. **SonarCloud begin** *(verticals that opt in via `conf.json`)* — `dotnet-sonarscanner begin` wraps the build/test that follow so the scanner can hook MSBuild's analyzers.
2. **Restore + build** — `dotnet build -c Release --no-incremental`. The build inside Sonar's begin/end window is what gives the scanner its data.
3. **Test + coverage (single pass)** — `dotnet-coverage collect` wraps `dotnet test` once, writing TRX reports plus a native `.coverage` binary. See
   [`TESTING_INFRASTRUCTURE_OVERHAUL/STEPS_PART_1/41_CI_Coverage_Single_Run.md`](TESTING_INFRASTRUCTURE_OVERHAUL/STEPS_PART_1/41_CI_Coverage_Single_Run.md).
4. **Convert coverage** — two `dotnet-coverage merge` calls turn the binary into cobertura (for the threshold check) and VSCoverage XML (for Sonar). No re-running tests.
5. **SonarCloud end** *(verticals that opt in)* — uploads the analysis. Runs even if tests failed so issues found by the scanner are still posted. See [../SONARCLOUD.md](../SONARCLOUD.md).
6. **Coverage threshold check** — parses the cobertura XML and fails the job if any enforced assembly is below its floor. See [COVERAGE.md](COVERAGE.md).
7. **Pack** — `dotnet pack` for `pkg`-type verticals only.
8. **Report failed tests** — post-test step that parses MTP logs and emits per-failure `::group::` + `::error title::` annotations on GitHub Actions.
9. **Upload artifacts** on failure — MTP `*.log` / `*.trx` files from `TestResults/`. Retention: 3 days. See
   [`TESTING_INFRASTRUCTURE_OVERHAUL/STEPS_PART_1/40_CI_First_Green_Run_Hardening.md`](TESTING_INFRASTRUCTURE_OVERHAUL/STEPS_PART_1/40_CI_First_Green_Run_Hardening.md).

## Microsoft Testing Platform (MTP)

xUnit v3 is self-hosted on MTP. A few things the pipeline relies on:

- Each test `.csproj` must set `<TargetFramework>net9.0</TargetFramework>`
  (**singular**). If it inherits a plural `<TargetFrameworks>` from a parent
  `Directory.Build.props`, `dotnet test` silently falls back to VSTest and
  reports *"No test is available in \<dll\>"*. See
  [`TESTING_INFRASTRUCTURE_OVERHAUL/STEPS_PART_1/37_CI_MTP_Routing_TargetFramework_Clear.md`](TESTING_INFRASTRUCTURE_OVERHAUL/STEPS_PART_1/37_CI_MTP_Routing_TargetFramework_Clear.md).
- MTP exit codes:
  - `0` — all tests passed
  - `8` — all tests were `[Skip]`ped (treated as success via
    `--ignore-exit-code 8`)
  - non-zero — failures

## Threshold enforcement scope

Threshold enforcement is scoped to the **owning vertical**: the Authorization
vertical only enforces thresholds for `Altinn.Authorization*` assemblies;
AccessManagement only enforces its own set. This prevents one vertical's CI
from failing because another vertical changed coverage numbers. Introduced in
[`TESTING_INFRASTRUCTURE_OVERHAUL/STEPS_PART_1/34_CI_Coverage_Threshold_Scoping.md`](TESTING_INFRASTRUCTURE_OVERHAUL/STEPS_PART_1/34_CI_Coverage_Threshold_Scoping.md).

## Container runtime

CI runners have Docker available, so Testcontainers-backed integration tests
(AccessManagement) run as normal. Verticals that don't need a container
(ABAC, PEP, Authorization.Tests) are unaffected.

If a container runtime is unavailable the fixtures `Assert.Skip(...)` —
individual tests show as skipped rather than failing the job. See
[`TESTING_INFRASTRUCTURE_OVERHAUL/STEPS_PART_1/40_CI_First_Green_Run_Hardening.md`](TESTING_INFRASTRUCTURE_OVERHAUL/STEPS_PART_1/40_CI_First_Green_Run_Hardening.md).

## Artifacts

On failure, each job uploads:

- `TestResults/*.log` — MTP stdout per test project
- `TestResults/*.trx` — machine-readable test results

Coverage artifacts (`*.cobertura.xml`) are **not** uploaded — the pipeline
uses them in-process for threshold checks and the raw data isn't valuable
post-hoc. (Decision: [`TESTING_INFRASTRUCTURE_OVERHAUL/STEPS_PART_1/40_CI_First_Green_Run_Hardening.md`](TESTING_INFRASTRUCTURE_OVERHAUL/STEPS_PART_1/40_CI_First_Green_Run_Hardening.md).)

## Blocked items

| Item | Blocker |
|---|---|
| `Altinn.Authorization.Host.Lease.Tests` in CI | Requires Azurite (Azure Storage emulator). Tracked in [`TESTING_INFRASTRUCTURE_OVERHAUL/STEPS_PART_1/INDEX.md#blocked-items`](TESTING_INFRASTRUCTURE_OVERHAUL/STEPS_PART_1/INDEX.md#blocked-items). |

## Debugging a CI test failure

1. Check the `Report failed tests` step in the failing job — it emits a
   GitHub annotation for each individual test failure.
2. Download the `TestResults` artifact for that job. The `*.coverage.log`
   and `*.log` files contain full MTP stdout, including per-test `[FAIL]`
   output.
3. For flaky tests, check whether the failure correlates with a specific OS
   (the pipeline runs Ubuntu for most verticals). Step 40 documents several
   Linux-specific issues that had to be fixed after the first green run.

## Related docs

- [COVERAGE.md](COVERAGE.md) — threshold mechanics and local-dev workflow.
- [../SONARCLOUD.md](../SONARCLOUD.md) — SonarCloud config, exclusions, per-vertical setup.
- [GETTING_STARTED.md](GETTING_STARTED.md) — local prerequisites.
- [`TESTING_INFRASTRUCTURE_OVERHAUL/STEPS_PART_1/INDEX.md`](TESTING_INFRASTRUCTURE_OVERHAUL/STEPS_PART_1/INDEX.md) — every CI change is logged here.
