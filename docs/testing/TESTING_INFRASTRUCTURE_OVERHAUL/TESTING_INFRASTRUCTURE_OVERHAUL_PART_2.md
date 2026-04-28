# Testing Infrastructure Overhaul — Part 2 (Audit & Plan)

> **Status:** 🟢 **Phase A complete (2026-04-27).** Audit sections 1–4
> populated by Step 1; Phase A's bundled critical fixes (C1', C2', C3',
> C5'; C4' dismissed) shipped under T1 issue
> [#2947](https://github.com/Altinn/altinn-authorization-tmp/issues/2947)
> in Steps 2–5; coverage baseline re-measured at Step 6 against the
> post-C5'-fix merged-cobertura view. Phases B–F still pending. Workflow
> lives in [`STEPS_PART_2/INDEX.md`](STEPS_PART_2/INDEX.md); the tracking
> conventions adopted from
> [`TRACKING_RETROSPECTIVE.md`](TRACKING_RETROSPECTIVE.md) are documented
> there. See the [Decision Log](#decision-log) below for what changed
> between the scaffold and this populated revision.
>
> **Predecessor:** [`TESTING_INFRASTRUCTURE_OVERHAUL_PART_1.md`](TESTING_INFRASTRUCTURE_OVERHAUL_PART_1.md)
> (Steps 1–61, closed).

---

## Why a Part 2?

By the end of Part 1 all of the originally identified issues (C1–C5,
M1–M8, L1–L3) were resolved and the phase plan was exhausted. The next
highest-value work was always going to be a **fresh audit** — many of the
baseline numbers have shifted materially since Step 12 and several new
areas (`Host.Pipeline`, `Host.Database`, `Host.MassTransit`, live-DB
Npgsql repository code, `Host.Lease` pending Azurite) were not yet covered
by any plan.

Rather than continue appending "steps 62+" to Part 1, we start a fresh
overhaul document so that:

- The scope of Part 2 stays crisp and auditable (a new list of IDs, e.g.
  C1'–C5', M1'–M8', L1'–L5').
- Part 1 remains a frozen historical record.
- Step numbering in `STEPS_PART_2/` starts at **Step 1** again, making
  cross-refs within Part 2 unambiguous.
- The Part 2 scaffold can adopt the tracking improvements identified in
  [`TRACKING_RETROSPECTIVE.md`](TRACKING_RETROSPECTIVE.md) from day one,
  rather than retrofitting them mid-phase.

---

## Table of Contents

1. [Current State Audit](#1-current-state-audit)
2. [Findings & Issues](#2-findings--issues)
3. [Best Practices Already Followed](#3-best-practices-already-followed)
4. [Improvement Plan — Phases](#4-improvement-plan--phases)
5. [Execution Order & Dependencies](#5-execution-order--dependencies)
6. [Tracking Conventions](#tracking-conventions)
7. [Decision Log](#decision-log)

---

## 1. Current State Audit

*Measured 2026-04-27 against branch `claude/peaceful-diffie-18e815` (base
`main` @ 5993088d). Coverage produced by
`eng/testing/run-coverage.ps1` over all 11 `*.Tests.csproj` projects;
results parsed and aggregated per-assembly across the 11 cobertura files
(max line%/branch% chosen — see §2 issue **C5'** for why the script's
own threshold check disagrees in workstation mode).*

### 1.1 Test project inventory

11 test projects, all xUnit v3 (2.0.3) on `net9.0` with MTP routing
enabled centrally. Numbers below are the **Step 6 re-baseline**
(post-T1 close); the original Step 1 anomalies (`ABAC.Tests` 0
discovered, 1 failing test in `Altinn.Authorization.Tests`) are
resolved.

| Project | Tests | Pass | Fail | Skip | Time | Notes |
|---|---:|---:|---:|---:|---:|---|
| `AccessMgmt.Tests` | 1196 | 1196 | 0 | 0 | ~131s | |
| `Altinn.AccessManagement.Api.Tests` | 58 | 58 | 0 | 0 | ~7s | |
| `Altinn.AccessManagement.Enduser.Api.Tests` | 351 | 342 | 0 | **9** | ~82s | L1' carry-overs |
| `Altinn.AccessManagement.ServiceOwner.Api.Tests` | 25 | 25 | 0 | 0 | ~59s | |
| `Altinn.AccessMgmt.Core.Tests` | 323 | 323 | 0 | 0 | ~39s | |
| `Altinn.AccessMgmt.PersistenceEF.Tests` | 41 | 41 | 0 | 0 | ~5s | |
| `Altinn.Authorization.Host.Lease.Tests` | 2 | 0 | 0 | **2** | ~0.2s | Azurite-blocked (M4') |
| `Altinn.Authorization.Host.Pipeline.Tests` ✨ | 1 | 1 | 0 | 0 | ~0.9s | NEW (Step 4 scaffold; Phase D.1 populates) |
| `Altinn.Authorization.Integration.Tests` | 45 | 40 | 0 | **5** | ~5s | L1' carry-overs |
| `Altinn.Authorization.PEP.Tests` | 92 | 92 | 0 | 0 | ~2s | |
| `Altinn.Authorization.Tests` | 402 | **402** ✅ | 0 | 0 | ~11s | C2' fixed in Step 5 |
| **Totals** | **2536** | **2520** | **0** | **16** | ~6.5 min wall-clock | |

Project-roster delta vs Step 1: `Altinn.Authorization.ABAC.Tests`
removed in Step 3 (was empty); `Altinn.Authorization.Host.Pipeline.Tests`
added in Step 4 (1 smoke test).

All 11 csproj files set `<TargetFramework></TargetFramework>` (empty) +
`<TargetFrameworks>net9.0</TargetFrameworks>` to defeat the singular TFM
inherited from `src/Directory.Build.props`, which would otherwise force
`dotnet test` back to VSTest and skip xUnit v3 discovery entirely (Part 1
Step 37). The MTP opt-in itself is set centrally — see §3.

All 11 csproj files set `<TargetFramework></TargetFramework>` (empty) +
`<TargetFrameworks>net9.0</TargetFrameworks>` to defeat the singular TFM
inherited from `src/Directory.Build.props`, which would otherwise force
`dotnet test` back to VSTest and skip xUnit v3 discovery entirely (Part 1
Step 37). The MTP opt-in itself is set centrally — see §3.

### 1.2 Fixture inventory

5 active fixtures + 1 static singleton (`PostgresServer`):

| Fixture | File | Scope | Container deps | Consumers |
|---|---|---|---|---|
| **`ApiFixture`** | [src/apps/Altinn.AccessManagement/test/Altinn.AccessManagement.TestUtils/Fixtures/ApiFixture.cs](../../src/apps/Altinn.AccessManagement/test/Altinn.AccessManagement.TestUtils/Fixtures/ApiFixture.cs) | per-test (`IAsyncLifetime`) | Postgres via `EFPostgresFactory` | 74 (dominant abstraction) |
| **`LegacyApiFixture`** | [src/apps/Altinn.AccessManagement/test/AccessMgmt.Tests/Fixtures/LegacyApiFixture.cs](../../src/apps/Altinn.AccessManagement/test/AccessMgmt.Tests/Fixtures/LegacyApiFixture.cs) | per-test (extends `ApiFixture`) | Postgres + Yuniql migrations | 5 (Dapper-repo / consent / delegation tests) |
| **`PostgresFixture`** | [src/apps/Altinn.AccessManagement/test/AccessMgmt.Tests/Fixtures/PostgresFixture.cs](../../src/apps/Altinn.AccessManagement/test/AccessMgmt.Tests/Fixtures/PostgresFixture.cs) | per-test via `PostgresServer` singleton | Postgres + Yuniql | 6 (low-level DB tests) |
| **`AuthorizationApiFixture`** | [src/apps/Altinn.Authorization/test/Altinn.Authorization.Tests/Fixtures/AuthorizationApiFixture.cs](../../src/apps/Altinn.Authorization/test/Altinn.Authorization.Tests/Fixtures/AuthorizationApiFixture.cs) | per-class (no `IAsyncLifetime`) | none (in-memory mocks) | 7 |
| **`PlatformFixture`** | [src/libs/Altinn.Authorization.Integration/test/Altinn.Authorization.Integration.Tests/PlatformFixture.cs](../../src/libs/Altinn.Authorization.Integration/test/Altinn.Authorization.Integration.Tests/PlatformFixture.cs) | plain class (manual lifecycle) | none | 5 |
| `PostgresServer` (static singleton) | (same file as `PostgresFixture`) | process-wide | Postgres | shared by `PostgresFixture` |

`ApiFixture` carries the docstring contract that won out in Part 1
Step 25: register `ConfigureServices` / seeds / appsettings overrides
**from the constructor only**, before the host is built; mid-test mutation
is silently ignored. `LegacyApiFixture` flips on
`PostgreSQLSettings:EnableDBConnection=true` + `RunIntegrationTests=true`
to opt the per-test database into the production Yuniql migration
pipeline at host startup.

### 1.3 Mock & test-helper inventory (selected — duplication flagged in §2 M1')

**`Altinn.AccessManagement.TestUtils/Mocks/`** (shared across
AccessManagement test projects): `Altinn2RightsClientMock`,
`AltinnRolesClientMock`, `DelegationChangeEventQueueMock`,
`PermitPdpMock`, `PolicyFactoryMock`, `PolicyRepositoryMock`,
`PolicyRetrievalPointMock`, `PolicyRetrievalPointWithWrittenPoliciesMock`,
`ProfileClientMock`, `PublicSigningKeyProviderMock`,
`ResourceRegistryClientMock`, `UserProfileLookupServiceMock` (12).

**`Altinn.AccessManagement.Tests/Mocks/`** + `Contexts/`: 14 additional
mocks specific to AccessMgmt integration tests (PDP variants, resource
registry, party/profile clients, configuration/JwtCookie stubs).

**`Altinn.Authorization.Tests/MockServices/`**: 15 mocks +
4 `*Stub` classes for the Authorization vertical.

**Confirmed near-duplicates** between AccessMgmt-TestUtils and
Authorization-MockServices: `PolicyRepositoryMock`,
`PolicyRetrievalPointMock`, `DelegationChangeEventQueueMock`,
`PublicSigningKeyProviderMock`, `ProfileClientMock` /
`ProfileMock`, `ResourceRegistryClientMock` / `ResourceRegistryMock`,
`JwtTokenMock`, `ConfigurationManagerStub`,
`JwtCookiePostConfigureOptionsStub` — at least 9, likely more once
behavior is diffed line-by-line.

### 1.4 Coverage baseline (line% / branch%, owned assemblies, merged cobertura)

**Re-baselined at Step 6** against the post-C5'-fix and post-Step-5
merged-cobertura view (`TestResults/coverage.cobertura.xml`). The
original Step 1 audit numbers used a max-across-files heuristic
because `run-coverage.ps1` produced 11 separate cobertura files and
the threshold script (pre-C5') couldn't aggregate; that heuristic
under-counted coverage whenever a test project covered different
lines of an assembly than another test project. The numbers below
are the true union — produced by `dotnet-coverage merge` then parsed
by the (now defensive) `check-coverage-thresholds.ps1`. **Five
assemblies improved by ≥ 9 pp purely from the measurement fix** (no
production-code change); see §1.6 for the breakdown.

Sorted by line%; **bold** = below 60% (audit threshold for "needs
work").

| # | Assembly | Line% | Branch% | Threshold | Δ vs Step 1 audit | Δ vs Part 1 final |
|---|---|---:|---:|---|---|---|
| 1 | `Altinn.AccessMgmt.PersistenceEF` | 99.03 | 95.53 | 90 (enf) | +0.25pp | +0.44pp |
| 2 | `Altinn.Authorization.Host` | 91.67 | 100.00 | — | ±0 | NEW |
| 3 | `Altinn.AccessManagement.Integration` | 88.35 | 85.71 | — | ±0 | +40.78pp ⬆️ |
| 4 | `Altinn.AccessManagement.Api.Maskinporten` | 80.36 | 80.00 | 75 (enf) | ±0 | ±0 |
| 5 | `Altinn.Authorization.PEP` | 79.60 | 83.48 | 75 (enf) | +0.61pp | +1.85pp |
| 6 | `Altinn.AccessManagement.Api.Enduser` | 73.88 | 65.36 | — | ±0 | +5.56pp |
| 7 | `Altinn.AccessManagement.Api.Internal` | **73.63** | **75.56** | — | **+25.07pp** 🎯 (was mis-counted) | +26.89pp ⬆️ |
| 8 | `Altinn.AccessManagement.Api.ServiceOwner` | 69.58 | 57.94 | — | ±0 | -2.16pp |
| 9 | `Altinn.Authorization` | 69.14 | 72.72 | 60 (enf) | +0.14pp (Step 5 dead-code removal) | -1.77pp |
| 10 | `Altinn.AccessManagement.Core` | 66.49 | 63.37 | 60 (enf) | +3.20pp | +3.06pp |
| 11 | `Altinn.AccessManagement.Api.Enterprise` | 66.39 | 56.52 | 60 (enf) | ±0 | ±0 |
| 12 | `Altinn.Authorization.ABAC` | 63.28 | 63.52 | 60 (enf) | +0.11pp | -0.13pp |
| 13 | **`Altinn.Authorization.Host.Database`** | **59.42** | 76.92 | — | ±0 | NEW (just under 60) |
| 14 | **`Altinn.AccessManagement` (main app)** | **57.57** | 60.64 | 60 (warn) | +1.10pp | -0.62pp ⬇️ |
| 15 | **`Altinn.AccessManagement.Persistence`** | **57.29** | 34.14 | — | +12.39pp 🎯 | +12.35pp |
| 16 | **`Altinn.Authorization.Integration.Platform`** | **54.94** | 69.08 | — | +9.56pp 🎯 | NEW |
| 17 | **`Altinn.AccessManagement.Api.Metadata`** | **51.53** | 51.11 | — | ±0 | +34.94pp ⬆️ |
| 18 | **`Altinn.AccessMgmt.Persistence`** | **47.32** | 34.27 | — | ±0 | +14.81pp (live-DB ceiling) |
| 19 | **`Altinn.AccessMgmt.Core`** | **44.96** | 35.05 | — | +11.30pp 🎯 | +27.65pp |
| 20 | **`Altinn.Authorization.Api.Contracts`** | **34.68** | 16.85 | — | +11.11pp | NEW (DTO-heavy — low-priority) |
| 21 | **`Altinn.AccessMgmt.Persistence.Core`** | **25.39** | 23.44 | — | +0.05pp | +16.61pp |
| 22 | **`Altinn.Authorization.Host.Lease`** | **6.87** | 4.17 | — | ±0 | BLOCKED on Azurite |
| 23 | **`Altinn.Authorization.Host.Pipeline`** | **0.24** | 0.00 | — | +0.24pp (Step 4 smoke test) | NEW (~1.4k LOC; populating tests is Phase D.1) |

`Altinn.Authorization.Host.MassTransit` (~157 LOC, mostly POCOs) is also
absent from coverage because nothing references it from the test tree.

### 1.5 Sub-60% classification

By how the gap can be closed (informs phase ordering in §4). Numbers
below are the Step 6 re-baseline.

- **Pure-logic reachable (no container)** — `Altinn.AccessMgmt.Core` (44.96%), `Altinn.AccessMgmt.Persistence.Core` (25.39%), `Altinn.AccessManagement.Api.Metadata` (51.53%), `Altinn.Authorization.Integration.Platform` (54.94% — `PaginatorStream<T>` + `TokenGenerator` deferred from Part 1), `Altinn.Authorization.Host.Database` (59.42%, small). **`Altinn.AccessManagement.Api.Internal` exited this list at 73.63% under proper aggregation** (Step 1 had it at 48.56% from the C5'-affected max-across-files heuristic).
- **Needs live DB (Npgsql repos)** — `Altinn.AccessMgmt.Persistence` (47.32%), `Altinn.AccessManagement.Persistence` (57.29% — much closer to 60% than Step 1 indicated). Both dominated by Dapper / `NpgsqlDataSource` repository code that can't be exercised without a real connection. Phase C target.
- **Needs Azurite** — `Altinn.Authorization.Host.Lease` (6.87%). Phase D.2 target.
- **No real tests yet** — `Altinn.Authorization.Host.Pipeline` (0.24% — only the Step 4 smoke test; ~1.4k LOC of substantive logic) and `Altinn.Authorization.Host.MassTransit` (no test project). Phase D.1 / D.3 target.
- **DTO/contracts** — `Altinn.Authorization.Api.Contracts` (34.68%). Mostly serialization records; minimal value in chasing.
- **Main-app shell** — `Altinn.AccessManagement` (57.57%). Mostly `Program.cs` startup + DI wiring, conventionally excluded from tight gating. **Regression vs Part 1 Step 12 (58.19%) is real but only -0.62pp under proper aggregation, not -1.72pp as Step 1 reported — see M2'.**

### 1.6 New sources of duplication / drift since Part 1 closed

- Mock duplication between vertical test trees (M1') was *flagged* in
  Part 1 Step 4 but never fully consolidated — entropy has grown rather
  than shrunk.
- **Most "regressions" Step 1 reported turned out to be measurement
  artefacts** of the C5' threshold-script bug. Re-baselined deltas vs
  Part 1 final:
  - `Altinn.AccessManagement.Api.ServiceOwner` -2.16pp (real — sole
    surviving regression).
  - `Altinn.Authorization` -1.77pp (was reported as -1.91pp; Step 5's
    dead-code removal recovers a small fraction).
  - `Altinn.AccessManagement` main app -0.62pp (was reported as
    -1.72pp; still a regression but materially smaller — investigate
    under M2').
- **Five assemblies under-counted by 9 pp+ purely from the C5'
  measurement bug**, with no production-code change between Part 1
  and Step 1:
  - `Altinn.AccessManagement.Api.Internal` +25.07pp (48.56% → 73.63%)
    — closes most of M6'.
  - `Altinn.AccessManagement.Persistence` +12.39pp (44.90% → 57.29%)
    — closer to 60% than M3' anticipated.
  - `Altinn.AccessMgmt.Core` +11.30pp (33.66% → 44.96%) — M5' still
    real but smaller.
  - `Altinn.Authorization.Api.Contracts` +11.11pp (23.57% → 34.68%)
    — DTO-heavy, low priority either way.
  - `Altinn.Authorization.Integration.Platform` +9.56pp (45.38% →
    54.94%) — M7' still real but smaller.

---

## 2. Findings & Issues

Fresh ID namespace (`C1'`–`C5'`, `M1'`–`M8'`, `L1'`–`L5'`) to keep the
audit cleanly distinct from Part 1.

### Critical (correctness, security, dead code)

- ~~**C1'** — **`Altinn.Authorization.ABAC.Tests` is an empty test
  project.**~~ — **Resolved by Part 2 Step 3** ([03_Delete_Empty_ABAC_Tests.md](STEPS_PART_2/03_Delete_Empty_ABAC_Tests.md)).
  Project + its orphan `test/Directory.Build.props` deleted; removed
  from the root and per-package solution files;
  [`docs/testing/TEST_PROJECTS.md`](../TEST_PROJECTS.md#pkg-abac)
  updated to note that ABAC is exercised indirectly via
  `Altinn.Authorization.Tests` (~63 % line / 61 % branch). The
  centrally-enforced 60 % `Altinn.Authorization.ABAC` threshold
  continues to gate that indirect coverage.
- ~~**C2'** — **1 failing test**: `ValidateParty_NotAsAuthenticatedUser_Forbidden`
  in [`PartiesControllerTest.cs:167`](../../src/apps/Altinn.Authorization/test/Altinn.Authorization.Tests/PartiesControllerTest.cs:167)
  expected `HTTP 403 Forbidden` but received `HTTP 200 OK`.~~ —
  **Resolved by Part 2 Step 5** ([05_Remove_AccessManagementAuthorizedParties_Flag.md](STEPS_PART_2/05_Remove_AccessManagementAuthorizedParties_Flag.md)).
  Triage outcome: the failing test was hitting the legacy `else`
  branch of `PartiesController.ValidateSelectedParty`, which was
  gated by the `AccessManagementAuthorizedParties` feature flag. The
  team architect confirmed the flag has been always-on in production
  for some time and is ready to be removed entirely. Step 5 deletes
  the flag, the legacy code paths it gated (the `else` branches in
  both `GetPartyList` and `ValidateSelectedParty`), and the
  now-orphaned `IParties.GetParties` / `IParties.ValidateSelectedParty`
  methods plus their `PartiesWrapper` / `PartiesMock` impls. With the
  legacy path gone the test passes naturally, and the originally-
  flagged "authorization regression" framing turns out to have been a
  test-hits-dead-code artefact rather than a security-relevant
  condition in production. **No security advisory needed** — the
  flag's always-on prod state means the bug was never reachable in
  the field.
- ~~**C3'** — **`Altinn.Authorization.Host.Pipeline` has no test project
  and 0% coverage** despite being ~1.4k LOC of substantive logic
  (hosted services, builders, segment/sink/source services, telemetry).
  Largest untested production library in the repo.~~ — **Scaffold
  landed in Part 2 Step 4** ([04_Scaffold_Host_Pipeline_Tests.md](STEPS_PART_2/04_Scaffold_Host_Pipeline_Tests.md)):
  new `Altinn.Authorization.Host.Pipeline.Tests` project with a single
  `PipelineMessage<T>` smoke test (one passing test confirms wiring + 
  prevents the C1' empty-project recurrence). The 0 %-coverage gap on
  the production assembly itself remains until Phase D.1 populates
  real tests; this step only paves the way.
- ~~**C4'** — **`AutoMapper 14.0.0` has a known high-severity vulnerability**
  (NU1903 / GHSA-rvv3-g6hj-g44x) — pinned in
  [`src/Directory.Packages.props:27`](../../src/Directory.Packages.props:27)
  and surfaced as a NU1903 warning in every restore of
  `Altinn.AccessManagement.csproj`. Investigate upgrade path (15.x?) or
  replacement.~~ — **Dismissed 2026-04-27.** AutoMapper transitioned from
  free to commercial-licensed in versions ≥ 15.x, so the upgrade path is
  blocked on a separate licensing decision; the specific advisory
  (GHSA-rvv3-g6hj-g44x) is not applicable to how this codebase uses
  AutoMapper. Restore-time NU1903 warning is accepted noise. Re-evaluate
  only if (a) the licensing situation changes, (b) AutoMapper publishes
  a free 14.x patch, or (c) usage patterns change in a way that makes
  the advisory relevant.
- ~~**C5'** — **`eng/testing/check-coverage-thresholds.ps1` produces
  false-positive threshold failures in workstation mode.**~~ —
  **Resolved by Part 2 Step 2** ([02_Fix_Coverage_Threshold_Aggregation.md](STEPS_PART_2/02_Fix_Coverage_Threshold_Aggregation.md)).
  `run-coverage.ps1` now invokes `dotnet-coverage merge` before the
  threshold check + ReportGenerator so each assembly appears exactly
  once; `check-coverage-thresholds.ps1` was additionally hardened with a
  two-phase aggregate-then-check (max line%/branch%, union Owned) so
  multi-file inputs no longer cascade into one spurious failure per
  per-test-project view. Side effect: the corrected aggregation
  reveals that the §1.4 baseline below *under-counted* several
  assemblies — see Step 2 verification for the deltas; the §1.4 table
  will be re-baselined when T1 (#2947) closes.

### Medium (coverage & cleanup)

- **M1'** — **Mock duplication between AccessManagement-TestUtils and
  Authorization-MockServices.** At least 9 near-identical mock/stub
  classes (see §1.3). Promote the canonical implementations into a
  cross-vertical helper (e.g. a new `Altinn.Authorization.TestCommon`
  package, or extend `Altinn.AccessManagement.TestUtils` and
  `ProjectReference` it from `Altinn.Authorization.Tests`).
- **M2'** — **`Altinn.AccessManagement` (main app) coverage regressed
  -0.62pp** (58.19% → 57.57%) since Part 1 Step 12 despite no
  test-deletion in Part 1's last 10 steps. Investigate which production
  files grew without matching tests; either add the tests or document
  the exclusion. *(Step 1 originally reported -1.72pp; the Step 6
  re-baseline shows the regression is materially smaller, but still
  real.)*
- **M3'** — **Live-DB Npgsql repository coverage.**
  `Altinn.AccessMgmt.Persistence` (47.32%) is the dominant remaining
  gap; `Altinn.AccessManagement.Persistence` (57.29% — was 44.90% in
  Step 1, the C5' fix surfaced +12.39pp of measurement under-counting)
  is now close enough to 60% that small additions could push it over.
  Adopt `EFPostgresFactory` template clones inside a dedicated xUnit
  collection scoped to repository tests (Part 1 Phase 2.2 pattern).
  This is Phase C of the Part 2 plan. *(Reduced urgency vs Step 1: the
  baseline gap is smaller than originally feared.)*
- **M4'** — **`Altinn.Authorization.Host.Lease` (6.87%) is blocked on
  Azurite**, carried over from Part 1 Phase 6.5. Add an Azurite
  Testcontainers fixture to `TestUtils` (mirroring the `PostgresServer`
  pattern with the same `Assert.Skip` outage guard). Phase D.2 of the
  Part 2 plan.
- **M5'** — **`Altinn.AccessMgmt.Core` (44.96%) and
  `Altinn.AccessMgmt.Persistence.Core` (25.39%) pure-logic gap.** Both
  ratcheted up significantly in Part 1 (Steps 42–60) but
  `Persistence.Core` remains very low. Identify remaining pure-logic
  targets (utility classes, builders, validation helpers).
- ~~**M6'** — **`Altinn.AccessManagement.Api.Internal` (48.56%) and
  `Altinn.AccessManagement.Api.Metadata` (51.53%) controller gaps.**~~
  — **`Api.Internal` resolved by Step 6 re-baseline** (true coverage
  73.63%; the Step 1 number was a C5' measurement artefact). Only
  `Altinn.AccessManagement.Api.Metadata` (51.53%) remains as a
  controller gap; direct Moq-based tests (proven in Part 1 Steps 49
  and 53) can close it. **Reduced scope**: M6' is now `Api.Metadata`
  only.
- **M7'** — **`Altinn.Authorization.Integration.Platform` (54.94%)** —
  `RequestComposer` / `ResponseComposer` covered in Part 1 Step 60;
  `PaginatorStream<T>` and `TokenGenerator` still deferred (thin logic
  / key-vault dependency). Re-evaluate. *(Step 1 reported 45.38%; the
  C5' fix surfaced +9.56pp.)*
- **M8'** — **`Altinn.Authorization.Host.Database` (59.42%, just under
  60%)** and **`Altinn.Authorization.Host.MassTransit` (no test project,
  ~157 LOC of POCOs)**. Decide per-library: add a thin smoke suite, fold
  into existing tests (e.g. covered transitively by `AccessMgmt.Tests`),
  or accept the gap as architectural glue.

### Low (housekeeping & ratchet)

- **L1'** — **16 skipped tests** across 3 projects:
  - 9 in `Altinn.AccessManagement.Enduser.Api.Tests` — including
    `Sender_ConfirmsDraftRequest_ReturnsPending` (Part 1 carry-over) and
    `Receiver_ApprovesPendingPackageRequest_ReturnsApproved` ([Skip]ped
    in Part 1 Step 62 with a rewrite TODO).
  - 5 in `Altinn.Authorization.Integration.Tests`.
  - 2 in `Altinn.Authorization.Host.Lease.Tests` (Azurite-blocked,
    unblocked by **M4'**).
  Audit each skip — some are environmentally blocked, some are
  cleanup-able.
- **L2'** — **Coverage threshold ratchet stale.** Promote new
  consistently-passing assemblies into the enforced list (numbers from
  the Step 6 merged-cobertura re-baseline; reliable now that C5' is
  fixed):
  - `Altinn.Authorization.Host` (91.67%) — set floor 85.
  - `Altinn.AccessManagement.Integration` (88.35%) — set floor 80.
  - `Altinn.AccessManagement.Api.Enduser` (73.88%) — set floor 70.
  - `Altinn.AccessManagement.Api.Internal` (73.63%) — set floor 70.
    *(NEW Phase F candidate — Step 1 had it at 48.56% under the
    C5'-affected heuristic.)*
  - `Altinn.AccessManagement.Api.ServiceOwner` (69.58%) — set floor 65.
  - Consider raising existing floors: Maskinporten 75→80 (now
    80.36%), PEP 75→78 (now 79.60%), AccessMgmt.PersistenceEF 90→95
    (now 99.03%).
- **L3'** — **`Altinn.AccessManagement` main-app warn-only ratchet at
  60% is currently *failing*** (57.57%) and regressing per **M2'**.
  Either drop the warn floor to 55% (interim, while M2' is investigated)
  or hold the line and treat the warning as an action item. *(Step 1
  reported the value as 56.47%; the Step 6 re-baseline shows the
  warn-floor delta is 2.43pp not 3.53pp — still a warning, but closer
  to closing.)*
- **L4'** — **Source-code `// See: ...` comments still use long Part 1
  paths** (`docs/testing/TESTING_INFRASTRUCTURE_OVERHAUL/STEPS_PART_1/<N>_*.md`).
  New tests must use the short form
  ([`STEPS_PART_2/INDEX.md` § Source-code cross-references](STEPS_PART_2/INDEX.md#source-code-cross-references));
  decide whether to also migrate the existing Part 1 comments
  in-place (sweep-able via a single Grep + Edit).
- **L5'** — **xUnit v2 packages still pinned** in
  [`src/Directory.Packages.props`](../../src/Directory.Packages.props)
  (`xunit` 2.9.3, `xunit.runner.visualstudio` 3.1.5) despite all 11 test
  projects being on v3. The conditional reference logic in
  [`src/Directory.Build.targets`](../../src/Directory.Build.targets)
  only adds these when `XUnitVersion=v2`, so the version pins are dead.
  Remove to prevent accidental version-mixing in any future test project
  that forgets to set `XUnitVersion=v3`.

---

## 3. Best Practices Already Followed

Carry forward from Part 1, plus newly observed:

- **All 11 test projects on a single stack** — xUnit v3 (2.0.3),
  `net9.0`, FluentAssertions 7.0.0, Moq 4.20.72, Microsoft.NET.Test.Sdk
  17.14.1. No mixed-version drift.
- **MTP routing centralized** in
  [`src/Directory.Build.targets:8-15`](../../src/Directory.Build.targets:8):
  when `IsTestProject=true` *and* `XUnitVersion=v3`, the targets file
  sets `OutputType=Exe` and
  `TestingPlatformDotnetTestSupport=true` (the official MS opt-in).
  Test projects only need to set `XUnitVersion=v3` (already inherited
  from each `test/Directory.Build.props`) plus the
  `<TargetFramework></TargetFramework>` empty-singular trick; everything
  else flows through.
- **Centralized `IsTestProject=true`** in every `test/Directory.Build.props`
  (5 of them, one per vertical). Every test project automatically gets
  `coverlet.collector`, `coverlet.msbuild`, `Microsoft.NET.Test.Sdk`,
  `xunit.v3`, `FluentAssertions`, `xunit` + `FluentAssertions` global
  usings — see
  [`src/Directory.Build.targets:25-62`](../../src/Directory.Build.targets:25).
- **Centralized package versions** via `Directory.Packages.props`
  (`ManagePackageVersionsCentrally=true`) — no per-project version
  pinning except in scoped subtree props.
- **Per-test DB isolation** via `EFPostgresFactory.Create()` /
  `PostgresServer.NewDatabase()` — each test gets a fresh cloned database
  on the shared Postgres container.
- **Docker-outage soft-skip** in
  [`PostgresServer.StartUsing`](../../src/apps/Altinn.AccessManagement/test/AccessMgmt.Tests/Fixtures/PostgresFixture.cs)
  — Testcontainers failures are converted to `Assert.Skip`, which surfaces
  as exit code 8 from MTP and is tolerated by CI's
  `--ignore-exit-code 8`.
- **Single-run CI** (Part 1 Step 41): one
  `dotnet-coverage collect -- dotnet test` invocation produces TRX +
  merged cobertura; `check-coverage-thresholds.ps1` parses-only. No
  duplicate test execution.
- **Vertical-scoped threshold enforcement** — assemblies are gated only
  if their source files live under the vertical's `OwnedRoot`; foreign
  assemblies are reported with `(ref)` for visibility but don't fail CI.
- **Auto `InternalsVisibleTo`** — non-test projects in the test build
  graph automatically get `InternalsVisibleTo($(AssemblyName).Tests)` via
  [`src/Directory.Build.targets:68`](../../src/Directory.Build.targets:68).
- **Shared fixture contract documented in code** — `ApiFixture`
  call-from-constructor-only contract (Part 1 Step 25) and
  `LegacyApiFixture` Yuniql-vs-pure-EF guidance are inline doc-commented.
- **Workstation parallel coverage collection** — `run-coverage.ps1`
  (Part 1 Step 41) runs each test project's `dotnet-coverage collect` in
  parallel, capping wall-clock at the slowest project (~2.5 min for
  `AccessMgmt.Tests`) instead of the serial sum.

---

## 4. Improvement Plan — Phases

Six phases, ordered by correctness/risk first then by effort.

### Phase A — Critical fixes (C1'–C5')

Block almost everything else until done. Small, well-scoped:

- ~~**A.1** — Resolve **C1'** `ABAC.Tests`: delete the empty project (and
  drop its INDEX.md / coverage row) OR add direct ABAC unit tests.~~ —
  **Done in Part 2 Step 3** (deleted; coverage stays indirect via
  `Altinn.Authorization.Tests`).
- ~~**A.2** — Resolve **C2'** failing `ValidateParty` test: triage — real
  auth regression in `PartiesController` vs mock drift; fix root cause,
  not the assertion.~~ — **Done in Part 2 Step 5** (architect confirmed
  the gating feature flag is always-on in prod and removable; deleted
  flag + legacy code paths; test passes naturally).
- ~~**A.3** — Resolve **C5'** `check-coverage-thresholds.ps1`
  false-positive: refactor to aggregate per-assembly across input files
  before threshold check.~~ — **Done in Part 2 Step 2**.
- ~~**A.4** — Investigate **C4'** AutoMapper 14.0.0 CVE.~~ — **Dropped**
  per dismissal of C4' (see §2). No work item.
- ~~**A.5** — Plan **C3'** `Host.Pipeline` test project: scaffold an empty
  `Altinn.Authorization.Host.Pipeline.Tests` project (xUnit v3 + the
  central wiring) without tests yet — paves the way for Phase D.~~ —
  **Done in Part 2 Step 4** (scaffolded with a single `PipelineMessage<T>`
  smoke test to avoid the C1' empty-project pattern).

### Phase B — Pure-logic coverage (M5', M6', M7')

Highest ROI per hour. No container, no external dependencies. Continues
the Part 1 6.7d pattern (Steps 42–60) of direct unit tests with
`InternalsVisibleTo` where needed.

**Re-scoped at Step 6** following the C5'-fix re-baseline: several
items shrank materially because Step 1's max-across-files heuristic
under-counted. Updated targets (largest gap first):

- **B.1** — `Altinn.AccessManagement.Api.Metadata` (51.53%) controller
  gap via Moq (M6'). *(`Altinn.AccessManagement.Api.Internal` was the
  other half of M6' but the Step 6 re-baseline shows it at 73.63%
  rather than the Step 1 figure of 48.56% — already passing the
  implicit 60% threshold, so dropped from B.1.)*
- **B.2** — `Altinn.AccessMgmt.Persistence.Core` (25.39% — largest
  remaining pure-logic gap) and `Altinn.AccessMgmt.Core` (44.96%)
  remaining pure-logic reachable code (M5'). Continues Part 1 Steps
  42–60.
- **B.3** — `Altinn.Authorization.Integration.Platform` (54.94%) tail —
  `PaginatorStream<T>` and (if feasible without key-vault)
  `TokenGenerator` (M7'). *(Step 1 reported 45.38%; the C5' fix
  surfaced +9.56pp.)*

### Phase C — Live-DB coverage (M3')

Largest unit of coverage work; requires fixture investment first:

- **C.1** — Design a `RepositoryDbCollection` xUnit collection that
  shares the `EFPostgresFactory` template-clone fixture across the
  Npgsql-repo tests in `Altinn.AccessMgmt.Persistence` (47.32%) and
  `Altinn.AccessManagement.Persistence` (44.90%).
- **C.2** — Write the per-repository test classes against the live
  Postgres instance.

### Phase D — New test projects (C3', M4', M8')

Each needs a new test project + initial smoke suite:

- **D.1** — `Altinn.Authorization.Host.Pipeline.Tests` — covers the ~1.4k
  LOC currently at 0%.
- **D.2** — Unblock `Altinn.Authorization.Host.Lease.Tests` by adding an
  Azurite Testcontainers fixture to `TestUtils` (M4').
- **D.3** — Decide on `Host.Database` (~150 LOC) and `Host.MassTransit`
  (~157 LOC): thin smoke suite vs accept-the-gap (M8').

### Phase E — Housekeeping & drift (M1', M2', L1', L4', L5')

Interleavable with B–D, can batch into a single PR each:

- **E.1** — Mock consolidation across verticals (M1').
- **E.2** — Investigate and address `AccessManagement` main-app
  coverage regression (M2').
- **E.3** — Audit the 16 skipped tests (L1').
- **E.4** — Rewrite source-code `// See: ...` comments to short form
  (L4').
- **E.5** — Drop dead xUnit v2 package pins (L5').

### Phase F — Coverage threshold ratchet (L2', L3')

Last; codifies the gains from Phases B–D. Promotes assemblies into the
enforced list and bumps existing floors. `Altinn.AccessManagement`
warn-only floor decision (L3') depends on M2' outcome.

---

## 5. Execution Order & Dependencies

```
Phase A (critical)  ─┬──> blocks Phase F (ratchet depends on script fix C5')
                     │
Phase B (pure-logic) ┼──> Phase F
                     │
Phase C (live-DB) ───┤    requires C.1 fixture before C.2
                     │
Phase D (new projects) requires A.5 scaffold for D.1
                     │
Phase E (housekeeping) — interleavable throughout
                     │
                     └──> Phase F (final)
```

Strict prerequisites:

- **A.3 (C5' fix)** before **F**: ratchet promotion requires reliable
  threshold checking.
- **A.5 (C3' scaffold)** before **D.1**: pipeline tests need the project
  to exist.
- **D.2 (Azurite fixture)** before resolving the 2 `Host.Lease.Tests`
  skips in **L1'**.
- **C.1 (DB-repo fixture)** before **C.2** (per-repo tests).

Everything else is independently schedulable.

---

## Tracking Conventions

The day-to-day tracking workflow lives in
[`STEPS_PART_2/INDEX.md`](STEPS_PART_2/INDEX.md). See its **Conventions**
section for:

- The [step-doc YAML front-matter template](STEPS_PART_2/INDEX.md#step-doc-template)
  required on every step doc.
- The [step-doc filename convention](STEPS_PART_2/INDEX.md#step-doc-filenames)
  (`<NN>_<Name>.md`, zero-padded).
- The [source-code cross-reference convention](STEPS_PART_2/INDEX.md#source-code-cross-references)
  (`// See: overhaul part-N step M`).
- The [`Phase/Tag`](STEPS_PART_2/INDEX.md#phasetag-column) and
  [`CovΔ (line)`](STEPS_PART_2/INDEX.md#cov%CE%B4-line-column) column rules
  for the step log.
- The [recommended structural-check script](STEPS_PART_2/INDEX.md#structural-check-recommended-early-step)
  for INDEX.md (good early Part 2 step, not a blocker).

The **~50-steps-per-part cap** is also enforced from the Part 2 INDEX:
when the step log approaches 50 entries, this document is closed and a
`PART_3` is opened rather than letting the table grow to Part 1's 61-row
length.

---

## Decision Log

| Date | Decision | Rationale |
|---|---|---|
| Step 61 (Part 1) | Start a Part 2 document for the fresh audit instead of extending Part 1 | Keeps Part 1 as a frozen historical record; new issue IDs get a clean namespace; step numbering restarts at 1. |
| 2026-04-27 (Part 2 scaffold) | Apply tracking improvements from [`TRACKING_RETROSPECTIVE.md`](TRACKING_RETROSPECTIVE.md) to the Part 2 scaffold *before* the kickoff audit | Cheaper than retrofitting mid-phase. Concrete changes: `Phase/Tag` + `CovΔ (line)` columns in the step-log table; YAML front matter required on every step doc; zero-padded step-doc filenames; `Last re-checked` column on Blocked Items; versioned (`v<date>`) handoff prompt; explicit ~50-step-per-part cap; short-form source-code cross-reference convention; structural-check script flagged as a recommended early step. Sidecar `steps.json` and an actual working linter script are deferred until a concrete consumer/incident motivates them. |
| 2026-04-27 (Part 2 Step 1) | Use **max-across-files** as the canonical per-assembly coverage number for the §1.4 baseline, not a merged cobertura | Workstation `run-coverage.ps1` produces 11 separate cobertura files. Until issue **C5'** is fixed, max line% per package is the cheapest conservative-upper-bound aggregation. CI's single-cobertura output is the long-term ground truth; once C5' lands the local-dev numbers will agree by construction. |
| 2026-04-27 (Part 2 Step 1) | Sequence Phase A (critical) before Phase F (ratchet promotion) | F (L2') depends on A.3 (C5' fix) — the threshold-check script is unreliable in workstation mode, so until it's fixed any ratchet promotion would be evaluated against false positives. |
| 2026-04-27 (Part 2 Step 5 rectification) | Replace Step 5's original cross-user-check hoist with full removal of the `AccessManagementAuthorizedParties` flag and its dead-code paths | The team architect confirmed the flag has been always-on in production for some time and is ready to retire. The originally-flagged "auth regression" was a test-hits-dead-code artefact (the test class flipped the mocked flag to `false` mid-class, exposing the legacy `else` branch); in the field, the legacy path was never reachable. Patching dead code is wasted effort — the right move is to delete it. The original Step 5 commit was force-rebased out of the branch's history before this PR opened; the corrected Step 5 commit deletes the flag entirely (and the now-orphaned `IParties.GetParties` / `ValidateSelectedParty` methods + their `PartiesWrapper` and `PartiesMock` impls). **No security advisory needed.** |
| 2026-04-27 (Part 2 Step 6) | Re-baseline §§1.1/1.4/1.5/1.6 against the post-C5'-fix merged-cobertura view; supersede the Step 1 max-across-files numbers | C5' is fixed (Step 2). Merged cobertura is the canonical view. Five assemblies were under-counted by 9pp+ purely from the Step 1 measurement bug — keeping Step 1's numbers would mis-direct Phase B effort (most notably toward `Api.Internal`, which is actually 73.63% not 48.56%). Original Step 1 numbers preserved in the Step 1 doc's `coverageDelta:` front-matter for historical reference. |
| 2026-04-27 (Part 2 Step 6) | Bundle T1's PR α (Steps 1–4 infra/docs) and PR β (Step 5 dead-code removal) into a single PR per user direction | User explicitly preferred fewer review cycles given the tech-lead-approval cost. The Step 5 change is single-purpose (delete the flag + its dead paths) and clearly callable-out in the PR body, so review-isolation isn't structurally needed. Tradeoff: tech lead reviews everything in one shot, but with an explicit "Flag removal" header so the production diff is easy to find. |

---

*Last updated 2026-04-27 by Step 6
(`STEPS_PART_2/06_T1_Closing_Sweep_and_Baseline_Refresh.md`) — closes
out T1 (#2947) and re-baselines §§1.1/1.4/1.5/1.6/2/4 against the
post-C5'-fix and post-Step-5 merged cobertura.*
