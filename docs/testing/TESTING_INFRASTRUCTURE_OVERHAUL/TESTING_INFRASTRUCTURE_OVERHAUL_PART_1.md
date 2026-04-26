# Testing Infrastructure Overhaul — Audit & Plan

> **Status:** ✅ **Complete** — all phases delivered across Steps 1–60.
> **Branches:** `feature/2842_Optimize_Test_Infrastructure_and_Performance` (Steps 1–41) → `feature/2842_Optimize_Test_Infrastructure_and_Performance_Part_Two` (Steps 42–60).
> **Step log:** See [`STEPS_PART_1/INDEX.md`](STEPS_PART_1/INDEX.md) for the ordered record of
> every step, including verification results and per-step docs.
>
> This document is retained as the original audit and issue ledger (IDs C1–C5,
> M1–M8, L1–L3). Check marks below point at the step(s) that resolved each
> item. Future work is tracked in `STEPS_PART_1/INDEX.md`, not here.

---

## Table of Contents

1. [Current State Audit](#1-current-state-audit)
2. [Findings & Issues](#2-findings--issues)
3. [Best Practices Already Followed](#3-best-practices-already-followed)
4. [Improvement Plan — Phases](#4-improvement-plan--phases)

---

## 1. Current State Audit

### 1.1 Test Projects Inventory

| # | Test Project | xUnit Version | Framework | Fixture Style | Has Testcontainers | Uses Moq |
|---|---|---|---|---|---|---|
| 1 | `AccessMgmt.Tests` | **v2** (default) | net9.0 | `IClassFixture<CustomWebApplicationFactory<T>>` + `IClassFixture<WebApplicationFixture>` + `IClassFixture<PostgresFixture>` | ✅ | ✅ |
| 2 | `Altinn.AccessManagement.Api.Tests` | **v3** | net9.0 | `IClassFixture<ApiFixture>` (from TestUtils) | ✅ | ✅ |
| 3 | `Altinn.AccessManagement.Enduser.Api.Tests` | **v3** | net9.0 | `IClassFixture<ApiFixture>` (from TestUtils) | ✅ | ✅ |
| 4 | `Altinn.AccessManagement.ServiceOwner.Api.Tests` | **v3** | net9.0 | `IClassFixture<ApiFixture>` (from TestUtils) | ✅ | ✅ |
| 5 | `Altinn.AccessMgmt.Core.Tests` | **v3** | net9.0 | References TestUtils | — | — |
| 6 | `Altinn.AccessMgmt.PersistenceEF.Tests` | **v3** | net9.0 | References TestUtils | — | — |
| 7 | `Altinn.Authorization.Tests` | **v2** (default) | net9.0 | `IClassFixture<CustomWebApplicationFactory<T>>` (own copy) | ❌ | ✅ |
| 8 | `Altinn.Authorization.Integration.Tests` | **v3** | net9.0 | `PlatformFixture` (manual DI, no WebApplicationFactory) | ❌ | ❌ |
| 9 | `Altinn.Authorization.Host.Lease.Tests` | **v2** (default) | net9.0 | — | ❌ | ❌ |
| 10 | `Altinn.Authorization.ABAC.Tests` | **v2** (default) | **net8.0** | — | ❌ | ❌ |
| 11 | `Altinn.Authorization.PEP.Tests` | **v2** (default) | net9.0 | Manual `new Mock<>()` setup | ❌ | ✅ |

### 1.2 Shared Test Infrastructure

| Component | Location | Notes |
|---|---|---|
| `Altinn.AccessManagement.TestUtils` | Shared test library (`IsTestLibrary`) | xUnit v3, contains `ApiFixture`, `EFPostgresFactory`, mocks, test data, token generator |
| `Directory.Build.targets` (root) | Auto-adds xunit, test SDK, coverlet based on `IsTestProject`/`IsTestLibrary` and `XUnitVersion` property | Clean conditional approach |
| `Directory.Build.props` per `test/` folder | Sets `IsTestProject=true` globally for all test projects under that folder | Good convention |

### 1.3 Fixture / WebApplicationFactory Variants

There are **4 distinct WebApplicationFactory implementations**:

1. **`CustomWebApplicationFactory<TStartup>`** — `AccessMgmt.Tests/CustomWebApplicationFactory.cs`  
   - Generic `<TStartup>`, old pattern typing the factory to a controller class.  
   - Reads `appsettings.test.json`, replaces `IAuditAccessor`.

2. **`WebApplicationFixture`** — `AccessMgmt.Tests/Fixtures/WebApplicationFixture.cs`  
   - Non-generic `WebApplicationFactory<Program>`, uses `PostgresServer` singleton.  
   - Has scenario-based mock composition (`ConfigureHostBuilderWithScenarios`).  
   - Creates full mock graph inline (PartiesClientMock, ProfileClientMock, etc.).

3. **`CustomWebApplicationFactory<TEntryPoint>`** — `Altinn.Authorization.Tests/Webfactory/CustomWebApplicationFactory.cs`  
   - Separate copy, nearly empty (no-op `ConfigureTestServices`).  
   - Each test class manually wires up all mocks via `WithWebHostBuilder`.

4. **`ApiFixture`** — `TestUtils/Fixtures/ApiFixture.cs`  
   - Modern, well-documented, composable (`ConfigureServices`, `WithAppsettings`, `EnsureSeedOnce<TKey>`).  
   - Uses `EFPostgresFactory` with template-database cloning for fast isolation.  
   - Used by the newer test projects (Enduser, ServiceOwner, Api.Tests).

### 1.4 Database Test Infrastructure

Two separate Postgres container strategies exist:

1. **`PostgresServer` (static)** — in `AccessMgmt.Tests/Fixtures/PostgresFixture.cs`  
   - Manual `PostgreSqlContainer` + Yuniql migrations.  
   - Creates numbered databases (`new_0`, `new_1`, …).  
   - Reference-counted start/stop via `Mutex` and `ConcurrentDictionary`.

2. **`EFPostgresFactory` (static)** — in `TestUtils/Factories/EFPostgresFactory.cs`  
   - Uses `CREATE DATABASE ... WITH TEMPLATE` for fast cloning.  
   - EF Core migrations + static data seeding on the template DB once.  
   - Much faster for per-test isolation.

### 1.5 Mock Duplication

Significant mock duplication exists:

| Mock Interface | `AccessMgmt.Tests/Mocks/` | `TestUtils/Mocks/` | `Authorization.Tests/MockServices/` |
|---|---|---|---|
| `PolicyRepositoryMock` | ✅ | ✅ | ✅ |
| `PolicyRetrievalPointMock` | ✅ | ✅ | ✅ |
| `PolicyFactoryMock` | ✅ | ✅ | — |
| `DelegationChangeEventQueueMock` | ✅ | ✅ | ✅ |
| `ResourceRegistryClientMock` | ✅ | ✅ | — |
| `ProfileClientMock` | ✅ | ✅ | — |
| `AltinnRolesClientMock` | ✅ | ✅ | — |
| `Altinn2RightsClientMock` | ✅ | ✅ | — |
| `SigningKeyResolver/Provider` | ✅ | ✅ | ✅ |

---

## 2. Findings & Issues

### 🔴 Critical Issues

| ID | Issue | Impact |
|---|---|---|
| **C1** | **Mixed xUnit v2 and v3 across projects.** `AccessMgmt.Tests`, `Authorization.Tests`, `Host.Lease.Tests`, `ABAC.Tests`, `PEP.Tests` use xUnit v2; the rest use v3. | Prevents using v3 features (skip via exception, `IAsyncLifetime` improvements, `TheoryData<T>` improvements). Runner conflicts possible. |
| **C2** | **`ABAC.Tests` still targets net8.0** while every other project targets net9.0. | Build matrix inconsistency. May mask net9.0-specific bugs. |
| **C3** | **4 different WebApplicationFactory patterns** with no shared base. | Developers don't know which to use. Bug fixes in fixture setup must be replicated. |
| **C4** | **Massive mock duplication** — same interfaces mocked 2-3 times across projects with diverging implementations. | Maintenance nightmare. Behavioral drift between test suites. |
| **C5** | **Two separate Postgres container/migration strategies** (Yuniql vs EF template cloning). | Confusing, doubles container startup cost in CI, inconsistent schema states. |

### 🟡 Moderate Issues

| ID | Issue | Impact |
|---|---|---|
| **M1** | **`CustomWebApplicationFactory<TController>` pattern** in `AccessMgmt.Tests` and `Authorization.Tests` — types the factory to a *controller* class rather than `Program`. | Misleading generic parameter. Each test class gets its own factory instance unnecessarily. |
| **M2** | **No `FluentAssertions` or assertion library** — raw `Assert.Equal`/`Assert.True` everywhere. | Verbose, poor failure messages, inconsistent assertion patterns. |
| **M3** | **`AcceptanceCriteriaComposer` pattern** — complex, home-grown test orchestration in `AccessMgmt.Tests`. | High learning curve. Mixes test data, HTTP call, and assertions into a single abstract class hierarchy. Not used in newer tests. |
| **M4** | **`[Collection("...")]` attributes** used inconsistently. Some controller tests use them, others don't. | Unintended test parallelism issues or unnecessary serialization. |
| **M5** | **Manual HTTP client setup** in `Authorization.Tests` — each test method calls `GetTestClient()` rebuilding the factory per-test. | Slow. Factory should be shared via fixture. |
| **M6** | **Test naming inconsistency** — mix of `Post_DeleteRules_Success`, `PDP_Decision_AltinnApps0001`, `HandleRequirementAsync_TC01Async`, descriptive vs opaque names. | Hard to understand failures without reading test code. |
| **M7** | **No code coverage enforcement** — `coverlet.collector` is included but no minimum threshold configured. | Coverage can silently regress. |
| **M8** | **Certificate files duplicated** — `platform-org.pem`, `selfSignedTestCertificate.pfx`, etc. copied in both `AccessMgmt.Tests` and `Authorization.Tests`. | Should be shared from TestUtils or a common location. |

### 🟢 Minor Issues

| ID | Issue | Impact |
|---|---|---|
| **L1** | `GlobalSuppressions.cs` in both `Authorization.Tests` and `PEP.Tests` suppressing analysis warnings. | Should be addressed at source or via `.editorconfig`. |
| **L2** | Empty `Folder` includes in `AccessMgmt.Tests.csproj` for directories that may not exist. | Build noise. |
| **L3** | `Compile Remove` and `None Remove` items in csproj hint at abandoned files. | Cleanup needed. |

---

## 3. Best Practices Already Followed

| Practice | Where |
|---|---|
| ✅ Central package management (`Directory.Packages.props`) | Root `src/` |
| ✅ Shared `Directory.Build.targets` auto-adding test SDK, xunit, coverlet | Root `src/` |
| ✅ `IsTestProject` / `IsTestLibrary` convention | All test `Directory.Build.props` |
| ✅ Testcontainers for real Postgres in integration tests | `AccessMgmt.Tests`, `TestUtils` |
| ✅ Template-database cloning for fast test isolation (`EFPostgresFactory`) | `TestUtils` |
| ✅ Dedicated `TestUtils` shared library for mocks and fixtures | `TestUtils` |
| ✅ `ApiFixture` with composable `ConfigureServices` / `EnsureSeedOnce` | `TestUtils` |
| ✅ `InternalsVisibleTo` auto-configured for `*.Tests` assemblies | `Directory.Build.targets` |
| ✅ Partial classes for large test classes (e.g., `ConnectionsControllerTest`) | `Enduser.Api.Tests` |
| ✅ `xunit.runner.json` present in test projects | Most projects |

---

## 4. Improvement Plan — Phases

### Phase 1: Foundation — Unify xUnit Version & Target Framework ✅
> **Goal:** Single xUnit version and TFM across all test projects. Resolves **C1**, **C2**.
> **Delivered by:** Step 2 ([2_Unify_xUnit_and_TFM.md](STEPS_PART_1/2_Unify_xUnit_and_TFM.md)).

- [x] **1.1** All test projects migrated to xUnit v3 via `<XUnitVersion>v3</XUnitVersion>` in each `test/Directory.Build.props`.
- [x] **1.2** `ABAC.Tests` now targets `net9.0` alongside every other project.
- [x] **1.3** xUnit v2 → v3 API breaks fixed (namespace, `TheoryData`, `Assert`, `IAsyncLifetime`).
- [x] **1.4** `Microsoft.NET.Test.Sdk` / `xunit.runner.visualstudio` removed where v3 self-hosting applies; later followed by MTP routing fixes in Steps 35–38.
- [x] **1.5** Full test suite verified green after migration.

### Phase 2: Consolidate WebApplicationFactory & Fixtures ✅
> **Goal:** Single shared fixture strategy for integration tests. Resolves **C3**, **C5**, **M1**, **M5**.
> **Delivered by:** Steps 3, 9, 16–26.

- [x] **2.1** `ApiFixture` (from `TestUtils`) adopted as the canonical integration fixture for all AccessManagement test projects.
- [x] **2.2** All `AccessMgmt.Tests` controller tests migrated off `CustomWebApplicationFactory<TController>` (Steps 16–20, 22–25).
- [x] **2.3** Scenario-based tests migrated to `ApiFixture` / `LegacyApiFixture` + composable seeding (Steps 22–25).
- [x] **2.4** `AuthorizationApiFixture` created for `Altinn.Authorization.Tests` (Step 9).
- [x] **2.5** `CustomWebApplicationFactory` (both copies), `WebApplicationFixture`, `AcceptanceCriteriaComposer`, and `Scenarios/*` retired (Steps 19, 26). `PostgresServer` retained only as a `LegacyApiFixture` implementation detail.
- [x] **2.6** Yuniql retained *only* behind `LegacyApiFixture` for the small tail of legacy consent tests that need the full Yuniql schema; the EF Core migration path is the default everywhere else. Full Yuniql removal is deferred until those tests are rewritten on EF seed data (tracked in `STEPS_PART_1/INDEX.md`).

### Phase 3: Deduplicate Mocks & Certificates ✅
> **Goal:** Single source of truth for each mock / certificate. Resolves **C4**, **M8**.
> **Delivered by:** Steps 4, 11, 15.

- [x] **3.1** Mock audit completed (Step 4).
- [x] **3.2** Canonical mocks consolidated in `TestUtils` (Step 15).
- [x] **3.3** All test projects now reference the shared `TestUtils` mocks.
- [x] **3.4** Duplicate mock files deleted (Step 15).
- [x] **3.5** Test certificates consolidated into `TestUtils/TestCertificates/` and duplicates removed (Step 11).

### Phase 4: Standardize Test Patterns & Naming ✅
> **Goal:** Consistent, readable, maintainable test code. Resolves **M2**, **M3**, **M4**, **M6**, **L1**, **L2**, **L3**.
> **Delivered by:** Steps 6, 10, 13, 14, 26, 27.

- [x] **4.1** Naming convention documented in [`TEST_NAMING_CONVENTION.md`](TEST_NAMING_CONVENTION.md) (Step 6).
- [x] **4.2** FluentAssertions evaluated (Step 13), adopted (Step 14), and documented in [`FLUENT_ASSERTIONS_GUIDELINES.md`](FLUENT_ASSERTIONS_GUIDELINES.md) (Step 27).
- [x] **4.3** `AcceptanceCriteriaComposer` retired entirely alongside `WebApplicationFixture` (Step 26).
- [x] **4.4** `[Collection]` usage standardized during the WAF consolidation (Steps 16–26).
- [x] **4.5** `GlobalSuppressions.cs` files cleaned up / moved to `.editorconfig` (Step 10).
- [x] **4.6** Dead `Compile Remove` / `None Remove` / empty `Folder` entries removed from csprojs (Steps 6, 10).

### Phase 5: Coverage Infrastructure ✅
> **Goal:** Measure and enforce code coverage. Resolves **M7**.
> **Delivered by:** Steps 5, 8, 28, 34–41.

- [x] **5.1** `dotnet-coverage` + `run-coverage.ps1` wired up (Step 5). Per-assembly thresholds configured in `eng/testing/coverage-thresholds.json` (Steps 8, 28).
- [x] **5.2** Cobertura reports produced and uploaded as CI artifacts (Steps 40, 41).
- [x] **5.3** Coverage gap backlog tracked in `STEPS_PART_1/INDEX.md` priority list.
- [x] **5.4** PR-gating `check-coverage-thresholds.ps1` added; single-run hybrid design eliminates duplicate test execution (Step 41). CI routing hardened across Steps 34–40.

### Phase 6: Maximize Code Coverage ✅
> **Goal:** Raise coverage across all source projects; enforce thresholds on the best-covered assemblies.
> **Delivered by:** Steps 7, 29–33, 42–60.

- [x] **6.1** `AccessMgmt.Core` — pure-logic coverage added across Parts 1–10 (Steps 42–46, 53–55, 59).
- [x] **6.2** `Altinn.AccessMgmt.PersistenceEF` at **98.59%** line / 90.78% branch (threshold 90%, enforced). `AccessMgmt.Persistence` / `AccessManagement.Persistence` remain Npgsql-dominated — non-Npgsql services (`AMPartyService`, `EntityService`, `PartyService`, `RoleService`, `RelationService`, `StatusService`, `AuditService`) covered in Steps 56, 58, 59. Live-DB Npgsql repository coverage is tracked as follow-up in `STEPS_PART_1/INDEX.md`.
- [x] **6.3** API endpoint coverage closed across `ServiceOwner` (Step 29, 47), `Enduser` (Steps 30–33, 57), `Api.Internal` (Steps 46, 49), `Api.Metadata` (Steps 42, 52), `Integration` (Steps 46, 50), `Integration.Platform` (Step 60).
- [x] **6.4** `Altinn.Authorization.Host.*` — addressed in Step 7. `Host.Lease` remains blocked on Azurite (see `STEPS_PART_1/INDEX.md` Blocked Items).
- [x] **6.5** `Altinn.Authorization.ABAC` 63.41% and `Altinn.Authorization.PEP` 77.75% — both above their enforced thresholds (Step 7).
- [x] **6.6** CI coverage thresholds are enforced per vertical (Steps 8, 28, 34). Latent production bugs uncovered while writing tests were fixed inline (Steps 47, 48, 51).

---

## Execution Order & Dependencies

```
Phase 1 (xUnit v3 + TFM)
    ↓
Phase 2 (Fixture consolidation)  ←  depends on Phase 1 for v3 IAsyncLifetime
    ↓
Phase 3 (Mock dedup)  ←  depends on Phase 2 for knowing which fixtures/projects reference which mocks
    ↓
Phase 4 (Patterns & naming)  ←  can partially run in parallel with Phase 3
    ↓
Phase 5 (Coverage infra)  ←  independent, can start after Phase 1
    ↓
Phase 6 (Max coverage)  ←  depends on Phases 2-5 being complete
```

---

## Decision Log

| Date | Decision | Rationale |
|---|---|---|
| Step 2 | Adopt xUnit v3 as the standard | Already used by newer projects; v3 is current; self-hosting simplifies csproj |
| Step 9 / 16 | `ApiFixture` is the canonical integration test fixture | Best-designed, already shared, supports composable seeding |
| Step 12+ | `EFPostgresFactory` (template cloning) over Yuniql (as default) | Faster, EF-native, already proven in newer tests |
| Step 15 | `TestUtils` is the shared test infrastructure library | Already exists, well-structured |
| Step 14 / 27 | Adopt `FluentAssertions` (7.0.0) for all new tests | Better failure messages, retires ~1,300 lines of bespoke `AssertionUtil` |
| Step 22 | Keep `LegacyApiFixture` (Yuniql) for the legacy consent tail | Cheaper than rewriting all legacy seed data against EF; fully retired `WebApplicationFixture` (Step 26) |
| Step 35 | Route CI `dotnet test` through Microsoft Testing Platform (MTP) for xUnit v3 | Required for v3 discovery |
| Step 41 | Hybrid CI design: single `dotnet-coverage collect -- dotnet test` run + parse-only threshold check | Removes ~4m32s serial coverage re-run |

---

## Completion Summary

All six phases are delivered. Open follow-up work — live-DB Npgsql repository
coverage, `Host.Lease` (blocked on Azurite), and a fresh infrastructure audit —
is tracked in [`STEPS_PART_1/INDEX.md`](STEPS_PART_1/INDEX.md) under *Recommended Next Steps*
and *Blocked Items*. This file is now a historical record; new work should
be logged as new steps, not as edits to this plan.

*Last updated at the end of Step 60.*
