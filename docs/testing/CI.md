# Tests in CI

**TL;DR:** [ci.yml](../../.github/workflows/ci.yml) builds and tests affected
verticals through RepoCtl. Scheduled SonarCloud analysis and coverage collection
have been removed; coverage remains available [locally](COVERAGE.md).

## Job structure

RepoCtl selects affected verticals with `repoctl ci find-verticals --filter full`.
Each selected vertical runs in a parallel matrix job:

1. Restore with `dotnet restore` in Release configuration.
2. Build with `dotnet build -c Release --no-restore`.
3. Test with `repoctl vertical test -c Release --no-build`.
4. Pack package and tool verticals with `dotnet pack -c Release --no-build`.
5. Build and push app containers and Flux manifest artifacts where configured.

The repository check job also runs `repoctl check`. A superseded PR run is
cancelled; push-to-main runs finish. After successful builds on main, the app
job promotes manifest artifacts to the configured test environments.

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
    lane selected 0 tests (treated as success via `--ignore-exit-code 8`)
  - non-zero — failures
- MTP's per-assembly summary line prints `Failed!` whenever nothing passed
  (`TotalFailed > 0 || TotalPassed == 0` in
  `Microsoft.Testing.Platform.MSBuild`'s `InvokeTestingPlatformTask`), so a
  zero-test or all-skipped run is logged as
  `Failed! - Failed: 0, Passed: 0, Skipped: 0, Total: 0` even when the exit
  code is mapped to success. There is no MTP option to change that display —
  the integration lane gate below exists to keep such runs from happening.

## Integration lane gate

A test assembly with no integration tests would still be executed by the
integration lane, match zero tests, and be logged as `Failed! ... Total: 0`
(see above). To keep the log truthful, such projects declare in their csproj:

```xml
<HasIntegrationTests>false</HasIntegrationTests>
```

When the integration lane runs (`dotnet test -p:TestLane=Integration`),
`src/Directory.Build.targets` imports
[`eng/testing/IntegrationLaneGate.targets`](../../eng/testing/IntegrationLaneGate.targets)
for these projects, which replaces the test invocation with a skip message.
Unit lanes, plain `dotnet test`, and IDE runs are unaffected — every assembly
always has unit tests (`TestCategoryGuard` itself is one), so only the
integration lane can select zero tests.

The declaration cannot go stale: `TestCategoryGuard` (compiled into every test
assembly, runs in the unit lane) fails when the declared value does not match
the assembly's actual tests — in both directions. Integration tests that are
permanently `[Skip]`ped do not count as runnable (an all-skipped run is also
logged as `Failed!`), which is why `Altinn.Authorization.Host.Lease.Tests`
declares `false` despite containing (perma-skipped) integration tests.

## Container runtime

CI runners have Docker available for Testcontainers-backed integration tests.
If a container runtime is unavailable locally, fixtures can skip tests rather
than fail. A successful run with skipped tests does not verify integration behaviour.

## Artifacts and debugging

Each vertical job uploads MSBuild binary logs from `binlog/*.binlog`, even on
failure, with one-day retention. Package and tool jobs also upload NuGet
packages with one-day retention. TRX reports are uploaded as
`test-results-<vertical slug>` artifacts with seven-day retention, including
when tests fail. xUnit v3 uses `--report-xunit-trx` through
`TestingPlatformCommandLineArguments`; xUnit v2 uses the VSTest TRX logger.

The [Test Report workflow](../../.github/workflows/ci__post-test-report.yml) runs after
CI and publishes a check per result artifact with test counts, failed test
details, up to 50 source annotations, and an Actions summary. It runs from
the default branch so fork PRs can receive checks without granting write
permissions to their test jobs. The reporting workflow must be merged into
the default branch before it starts running. Missing reports are allowed for
verticals without tests or jobs that failed before testing.

For a test failure, inspect the test report check or the job's `Test` step,
which enables `TestingPlatformShowTestsFailure` to print individual errors.
Download the TRX or binlog artifact when more detail is needed.
Reproduce with the same vertical and
Release configuration locally, with a container runtime available for integration tests.

## Related docs

- [COVERAGE.md](COVERAGE.md) — local coverage workflow and targets.
- [../SONARCLOUD.md](../SONARCLOUD.md) — retained analysis configuration.
- [GETTING_STARTED.md](GETTING_STARTED.md) — local prerequisites.
