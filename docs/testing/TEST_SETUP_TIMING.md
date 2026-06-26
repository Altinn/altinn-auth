# Integration-test setup time

How integration-test setup time is kept down. Measurement detail lives in
[#3379](https://github.com/Altinn/altinn-authorization-tmp/issues/3379).

## Where the time goes

Two things dominate integration-test setup:

- **Per-class `WebApplicationFactory` host builds.** Each `IClassFixture` builds
  its own host (DI graph + EF model) — the single largest cost in
  `AccessMgmt.Tests`.
- **Per-fixture database provisioning.** In the other AccessManagement test
  assemblies (`Enduser.Api.Tests`, `ServiceOwner.Api.Tests`,
  `AccessMgmt.Core.Tests`) this dominates instead — mostly time spent blocked on
  the engine's one-time template build at startup, not per-fixture work. The
  template (migrate + seed) is built once per process; each test database is a
  fast clone of it.

So the levers are **fewer host builds** (fixture sharing) and **not serialising
every fixture behind the one-time template build**.

## Fixture sharing

A cohort of read-only test classes can share one host via `ICollectionFixture`
instead of one `IClassFixture` each, collapsing N cold host builds into one.

A class may join a shared collection only if it:

1. seeds additively via `EnsureSeedOnce<TSelf>`,
2. never calls `ConfigureServices` / `WithAppsettings` / `With*FeatureFlag`,
3. issues no writes against rows other members read (write detection must match
   `PostAsJsonAsync` / `SendAsync`, not just `PostAsync`), **and**
4. asserts scoped / subset results — not exact-equals or counts that another
   member's additive seed would change.

Conditions 1–3 are statically checkable; 4 only surfaces at runtime, so run a
cohort once before keeping it. Anything that fails the rule keeps its own
`IClassFixture`.

Shared cohorts today: `ConnectionsController` and `RequestController` in
`Enduser.Api.Tests`. They are the only multi-class, all-read-only controllers in
the suite — everything else is one class per controller, or has a writer or a
`ConfigureServices` / feature-flag member, so there is no cohort to share.

## Parallelism

`maxParallelThreads` is **4**. The limiter is the single shared Postgres
container (clone throughput + connection pool), not CPU, so more threads add
contention without speeding the suites up — including host-build-heavy ones.

## Test waits

Test code uses **no `Thread.Sleep`**. `Task.Delay` is allowed only for bounded
shutdown-timeout guards and deliberate test-double latency; a delay-then-assert
pattern must instead poll for the observable signal (e.g.
`MeasurementCollector.WaitForMeasurementsAsync`).

## Reproduce

`FixtureTiming` (opt-out via `FIXTURE_TIMING=off`) writes one summary line per
test process to stdout, and appends it to `FIXTURE_TIMING_FILE` when that is set
(stdout is swallowed by MTP / `dotnet-coverage` in CI). CI prints the
per-assembly breakdown in the test job log automatically. Locally:

```bash
FIXTURE_TIMING_FILE="$PWD/fixture-timing.txt" \
DOCKER_HOST="npipe://./pipe/podman-machine-default" \
TESTCONTAINERS_RYUK_DISABLED=true \
dotnet test src/apps/Altinn.AccessManagement/test/AccessMgmt.Tests/AccessMgmt.Tests.csproj \
  -c Release -- --filter-trait "Category=Integration" --ignore-exit-code 8
```
