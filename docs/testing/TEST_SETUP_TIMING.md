# Integration-test setup timing (measurement)

Measurement step for [#3379](https://github.com/Altinn/altinn-authorization-tmp/issues/3379):
size where integration-test wall-clock goes **before** investing in a
fixture-sharing refactor. Instrumented by `FixtureTiming` (linked alongside
`PostgresTestEngine`); see [#3446](https://github.com/Altinn/altinn-authorization-tmp/issues/3446).

## Result

`AccessMgmt.Tests`, `Category=Integration` — **972 tests, ~2m05s**, run locally
against Podman (`maxParallelThreads = 4`):

| Phase | Total | Count | Avg | Notes |
|---|---:|---:|---:|---|
| App-host build (`WebApplicationFactory`) | **92.2 s** | 68 | **1,356 ms** | one per `IClassFixture` |
| DB provision (`InitializeAsync` → factory) | 56.2 s | 68 | 826 ms | includes template + clone |
| DB clone (`CREATE DATABASE … WITH TEMPLATE`) | **18.4 s** | 80 | **230 ms** | per provisioned database |
| Template build (migrate + seed) | 9.3 s | 1 | — | one-time per process |

## Conclusion

- **Host build dominates setup — ~5× the total clone cost (92 s vs 18 s).**
  The DB layer is already cheap: a clone is ~230 ms, and the migrated/seeded
  template is built once (~9 s). This confirms the #3379 hypothesis — the
  bottleneck is the **68 unshared `WebApplicationFactory` host builds**, not the
  database.
- The first host build is much slower (~13 s cold, JIT + EF model warm-up); the
  amortized average across 68 fixtures is ~1.36 s.
- This run covers `AccessMgmt.Tests` only (68 host-building fixtures). The other
  AccessManagement test projects add more `ApiFixture` instances — the
  repo-wide figure cited in #3379 is 111 `IClassFixture` / 85 `ApiFixture`,
  every one a separate cold host build.

## Prototype: fixture sharing (validated)

Sharing one host across a read-only, additive-seed cohort via
`ICollectionFixture` collapses N cold host builds into one. Prototyped on the
`ConnectionsController` cluster (`Enduser.Api.Tests`): the **7** nested classes
that are read-only, do not call `ConfigureServices`, and apply no per-class
configuration (`CheckPackage`, `DelegationCheckRoles`, `GetAvailableUsers`,
`GetConnections`, `GetInstances`, `GetPackages`, `GetRoles`) now join a single
`ConnectionsReadOnlyCollection`. The other classes — `Add*`/`Remove*` (mutating)
and any calling `ConfigureServices` — stay on `IClassFixture`.

| Metric | Before | After |
|---|---:|---:|
| Test result | 143 passed / 5 skipped | **143 passed / 5 skipped** |
| Wall clock | 1m 02s | **35.8 s** (−42%) |
| Host builds (`host_build_n`) | 25 | **19** (−6) |
| Host-build work | 129 s | 52 s |

Identical pass/skip counts — no seed collisions, no isolation regression. The
saving exceeds the 6 eliminated builds because removing them also cut CPU
contention on the remaining 19 (avg build 5.2 s → 2.8 s). Clone/template work is
untouched; it is already cheap.

**Shareability rule.** A class can join a shared collection only if it:

1. seeds additively via `EnsureSeedOnce<TSelf>`,
2. never calls `ConfigureServices` / `WithAppsettings` / `With*FeatureFlag`,
3. issues no writes against rows other members read, **and**
4. asserts *scoped / subset* results — not exact-equals or counts that another
   member's additive seed would change.

Conditions 1–3 are statically checkable; **4 is not** — it only surfaces at
runtime, so every converted cohort is run before it is kept. Anything failing the
rule keeps its own `IClassFixture`.

## Rollout status

| Cohort | Classes shared | Result |
|---|---:|---|
| `ConnectionsController` (Enduser) | 7 | ✅ shared |
| `RequestController` (Enduser) | 4 | ✅ shared |
| `ClientDelegationController` (Enduser) | 0 | ⛔ reverted — `GetClients`/`GetAgents` assert exact results and failed under sharing (condition 4) |

`AuthorizedParties` and the Maskinporten controllers are excluded statically
(`ConfigureServices` / per-class feature flags). Full Enduser integration suite
after sharing the two safe cohorts: **221 passed / 9 skipped, 0 failed.**

**Survey of the remaining test projects (complete).** The lever needs *multiple*
read-only classes in one controller. The rest of the AccessManagement suite does
not offer this:

- `AccessMgmt.Tests` is almost entirely one class per controller — nothing to
  share.
- Every other multi-class file (`ServiceOwner.Api.Tests` `RequestController` /
  `ServiceOwnerConnections`, `AccessMgmt.Tests` `MaskinportenSchema` /
  `RightsInternal`) contains a writer or a `ConfigureServices` / feature-flag
  member, leaving at most one read-only class — no cohort.

So `ConnectionsController` + `RequestController` (Enduser) are the only
safely-shareable read-only cohorts in the suite. **Note:** detection of writes
must match `PostAsJsonAsync` / `SendAsync`, not just `PostAsync` — a class that
creates data (even a `Get…Then…` setup) is not shareable.

## Parallelism (measured — no change warranted)

`maxParallelThreads` sweep on `AccessMgmt.Tests` integration (972 tests, local
Podman, 20-core host), all green:

| `maxParallelThreads` | Duration | vs current |
|---:|---:|---:|
| 1 | 2m 55s | +49% |
| **4 (current)** | **1m 57s** | — |
| 8 | 2m 05s | +7% |
| 16 | 1m 58s | +1% |

The setting *is* honoured (1 → 4 is ~33% faster), but **4 is already at the
plateau** — 4 → 8 → 16 is flat despite 20 idle cores. The limiter is not CPU; it
is the single shared Postgres container (clone creation + connection throughput,
`MaxPoolSize = 50`) plus the CPU-bound host builds. So raising
`maxParallelThreads` only adds shared-state / connection pressure (cf. the #3376
seed race) for no measured gain.

**Decision: keep `maxParallelThreads: 4`.** The effective lever for these suites
is fewer host builds (fixture sharing, above), not more threads. Going faster
would require removing the single-container bottleneck (e.g. a container per
worker) — out of scope and unlikely to pay off.

## Test waits (`Task.Delay` audit)

Test code has **0 `Thread.Sleep`** and **32 `Task.Delay`**. Audited:

| Use | Count | Action |
|---|---:|---|
| `Task.WhenAny(serviceTask, Task.Delay(1000))` shutdown timeout guards | 12 | keep (bounded wait) |
| `Task.Delay(N) // simulate processing` inside mock callbacks | 6 | keep (intended test-double latency) |
| 100M-ms cancellation sentinel / 90 s lease-TTL / SUT retry backoff | 3 | keep (not test waits) |
| `Task.Delay(20)` then assert metrics (`ConsentServiceTests`) | 4 | **fixed** → `MeasurementCollector.WaitForMeasurementsAsync` (polls; returns on first measurement, throws on timeout) |
| `Task.Delay(10)` "let the hosted-service loop reach its timer" (`ConsentMigrationHostedServiceTests`) | 7 | keep — driven by a fake `TimeProvider`; nothing signals "now awaiting the timer", so a poll has no observable state to watch |

Net: the one genuinely flaky *delay-then-assert* pattern (the metrics listener)
is replaced with polling. The rest are either correct by design (bounded guards,
deliberate latency) or lack an observable signal to poll on.

## Reproduce

`FixtureTiming` is opt-out (disable with `FIXTURE_TIMING=off`) and writes one
summary line to stdout and, when `FIXTURE_TIMING_FILE` is set, appends it to
that file (stdout is swallowed by MTP / `dotnet-coverage` in CI).

```bash
# Local run against a container runtime (here: Podman; Docker Desktop works too).
FIXTURE_TIMING_FILE="$PWD/fixture-timing.txt" \
DOCKER_HOST="npipe://./pipe/podman-machine-default" \
TESTCONTAINERS_RYUK_DISABLED=true \
dotnet test src/apps/Altinn.AccessManagement/test/AccessMgmt.Tests/AccessMgmt.Tests.csproj \
  -c Release -- --filter-trait "Category=Integration" --ignore-exit-code 8
```

Numbers are from local Podman; absolute values differ on CI hardware, but the
**ratio** (host build ≫ clone) is the hardware-independent finding.
