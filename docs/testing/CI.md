# Tests in CI

Tests run per-vertical in parallel jobs. Each vertical (app/lib/pkg) has its
own CI job driven by a shared template: `tpl-vertical-ci.yml`. Build, test,
threshold enforcement, and SonarCloud analysis all happen in one job per
vertical (`build-test-analyze`) so the test suite executes exactly once.

## Job structure

For each vertical the pipeline runs:

1. **SonarCloud begin** *(verticals that opt in via `conf.json`)* — `dotnet-sonarscanner begin` wraps the build/test that follow so the scanner can hook MSBuild's analyzers.
2. **Restore + build** — `dotnet build -c Release --no-incremental`. The build inside Sonar's begin/end window is what gives the scanner its data.
3. **Test + coverage (two lanes off one build)** — tests run in two
   sequential lanes selected by the `Category` trait: a fast **unit** lane
   (`--filter-trait "Category=Unit"`) then a slower **integration** lane
   (`--filter-trait "Category=Integration"`). Both reuse the single build (`--no-build`) and
   each is wrapped by `dotnet-coverage collect` into its own `.coverage`
   binary (`coverage.unit.coverage` / `coverage.integration.coverage`) and
   writes TRX into a per-lane `TestResults/<lane>/` subdir. The integration
   lane runs even if the unit lane failed (but not if the build failed) so
   coverage spans the whole suite. The Convert steps below merge
   `TestResults/*.coverage` into one report, so the threshold gate and Sonar
   still see whole-suite coverage. A lane that selects 0 tests in a
   single-type vertical (e.g. `pkg: PEP` has no integration tests) exits 8,
   which `--ignore-exit-code 8` treats as success.
4. **Convert coverage** — two `dotnet-coverage merge` calls turn the binary into cobertura (for the threshold check) and VSCoverage XML (for Sonar). No re-running tests.
5. **SonarCloud end** *(verticals that opt in)* — uploads the analysis. Runs even if tests failed so issues found by the scanner are still posted. See [../SONARCLOUD.md](../SONARCLOUD.md).
6. **Coverage threshold check** — parses the cobertura XML and fails the job if any enforced assembly is below its floor. See [COVERAGE.md](COVERAGE.md).
7. **Pack** — `dotnet pack` for `pkg`-type verticals only.
8. **Report failed tests** — post-test step that parses MTP logs and emits per-failure `::group::` + `::error title::` annotations on GitHub Actions.
9. **Upload artifacts** on failure — MTP `*.log` / `*.trx` files from `TestResults/`. Retention: 3 days.

## Microsoft Testing Platform (MTP)

xUnit v3 is self-hosted on MTP. A few things the pipeline relies on:

- Each test `.csproj` **clears** the singular `<TargetFramework>` it inherits
  from `src/Directory.Build.props` and sets
  `<TargetFrameworks>net10.0</TargetFrameworks>` (**plural**, single-valued) —
  that routing is what `dotnet test` needs to reach the MTP runner for xUnit v3.
  If the inherited singular `<TargetFramework>` is left non-empty, `dotnet test`
  silently falls back to VSTest and reports *"No test is available in \<dll\>"*.
- MTP exit codes:
  - `0` — all tests passed
  - `8` — no tests ran: either all were `[Skip]`ped, or a `--filter-trait`
    lane selected 0 tests in a single-type vertical (treated as success via
    `--ignore-exit-code 8`)
  - non-zero — failures

## Threshold enforcement scope

Threshold enforcement is scoped to the **owning vertical**: the Authorization
vertical only enforces thresholds for `Altinn.Authorization*` assemblies;
AccessManagement only enforces its own set. This prevents one vertical's CI
from failing because another vertical changed coverage numbers.

## Container runtime

CI runners have Docker available, so Testcontainers-backed integration tests
(AccessManagement, and the Authorization delegation-metadata repository tests)
run as normal. Verticals that don't need a container (ABAC, PEP) are unaffected.

If a container runtime is unavailable the fixtures `Assert.Skip(...)` —
individual tests show as skipped rather than failing the job.

## Artifacts

On failure, each job uploads:

- `TestResults/*.log` — MTP stdout per test project
- `TestResults/*.trx` — machine-readable test results

Coverage artifacts (`*.cobertura.xml`) are **not** uploaded — the pipeline
uses them in-process for threshold checks and the raw data isn't valuable
post-hoc.

## Blocked items

| Item | Blocker |
|---|---|
| `Altinn.Authorization.Host.Lease.Tests` in CI | Requires Azurite (Azure Storage emulator). |

## Debugging a CI test failure

1. Check the `Report failed tests` step in the failing job — it emits a
   GitHub annotation for each individual test failure.
2. Download the `TestResults` artifact for that job. The `*.coverage.log`
   and `*.log` files contain full MTP stdout, including per-test `[FAIL]`
   output.
3. For flaky tests, check whether the failure correlates with a specific OS
   (the pipeline runs Ubuntu for most verticals) — several Linux-specific
   issues had to be fixed after the suite first went green.

## Related docs

- [COVERAGE.md](COVERAGE.md) — threshold mechanics and local-dev workflow.
- [../SONARCLOUD.md](../SONARCLOUD.md) — SonarCloud config, exclusions, per-vertical setup.
- [GETTING_STARTED.md](GETTING_STARTED.md) — local prerequisites.
