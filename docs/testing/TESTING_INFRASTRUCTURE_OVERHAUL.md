# Testing Infrastructure Overhaul — Audit & Plan

> **Branch:** `feature/2842_Optimize_Test_Infrastructure_and_Performance`  
> **Created:** Auto-generated audit of the current testing infrastructure.

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

### Phase 1: Foundation — Unify xUnit Version & Target Framework
> **Goal:** Single xUnit version and TFM across all test projects.

- [ ] **1.1** Migrate all test projects to **xUnit v3** by setting `<XUnitVersion>v3</XUnitVersion>` in each `test/Directory.Build.props` (or remove the property where it defaults to v2).
  - `src\apps\Altinn.AccessManagement\test\Directory.Build.props` — add `<XUnitVersion>v3</XUnitVersion>`
  - `src\apps\Altinn.Authorization\test\Directory.Build.props` — add `<XUnitVersion>v3</XUnitVersion>`
  - `src\libs\Altinn.Authorization.Host\test\Directory.Build.props` — add `<XUnitVersion>v3</XUnitVersion>`
  - `src\pkgs\Altinn.Authorization.ABAC\test\Directory.Build.props` — add `<XUnitVersion>v3</XUnitVersion>`
  - `src\pkgs\Altinn.Authorization.PEP\test\Directory.Build.props` — add `<XUnitVersion>v3</XUnitVersion>`
- [ ] **1.2** Update `ABAC.Tests` to target `net9.0` (align with all other projects).
- [ ] **1.3** Fix any xUnit v2 → v3 API breaking changes (namespace changes, `TheoryData`, `Assert` API differences, `IAsyncLifetime` signature changes).
- [ ] **1.4** Remove `Microsoft.NET.Test.Sdk` and `xunit.runner.visualstudio` from projects that no longer need them with xUnit v3 (v3 is self-hosting).
- [ ] **1.5** Verify all tests still pass after migration.

### Phase 2: Consolidate WebApplicationFactory & Fixtures
> **Goal:** Single shared fixture strategy for integration tests.

- [ ] **2.1** Adopt `ApiFixture` from `TestUtils` as the **canonical** integration test fixture for all AccessManagement test projects.
- [ ] **2.2** Migrate `AccessMgmt.Tests` controller tests from `CustomWebApplicationFactory<TController>` to `ApiFixture` pattern.
- [ ] **2.3** Migrate `WebApplicationFixture` scenario-based tests to use `ApiFixture` + composable seeding.
- [ ] **2.4** Create an equivalent shared fixture for `Altinn.Authorization.Tests` (or extend `ApiFixture` if the apps share enough infrastructure).
- [ ] **2.5** Retire `CustomWebApplicationFactory` files and `PostgresFixture`/`PostgresServer` once all consumers are migrated.
- [ ] **2.6** Remove the Yuniql migration path — consolidate on EF Core migrations only.

### Phase 3: Deduplicate Mocks
> **Goal:** Single source of truth for each mock.

- [ ] **3.1** Audit all mocks in `AccessMgmt.Tests/Mocks/`, `TestUtils/Mocks/`, and `Authorization.Tests/MockServices/` — identify canonical versions.
- [ ] **3.2** Move canonical mocks to `TestUtils` (or create a new `Altinn.Authorization.TestUtils` shared library for the Authorization app).
- [ ] **3.3** Update all test projects to reference the shared mock implementations.
- [ ] **3.4** Delete duplicate mock files.
- [ ] **3.5** Consolidate shared test certificates into `TestUtils` and remove duplicates.

### Phase 4: Standardize Test Patterns & Naming
> **Goal:** Consistent, readable, maintainable test code.

- [ ] **4.1** Define and document a test naming convention (e.g., `MethodName_Scenario_ExpectedResult` or structured format).
- [ ] **4.2** Evaluate adding **FluentAssertions** (or **Shouldly**) for richer assertion messages; add to `Directory.Packages.props` if adopted.
- [ ] **4.3** Review `AcceptanceCriteriaComposer` pattern — decide whether to keep, simplify, or replace with standard `[Theory]` + `MemberData` + shared seeding.
- [ ] **4.4** Standardize `[Collection]` usage — document when to serialize vs parallelize.
- [ ] **4.5** Clean up `GlobalSuppressions.cs` files — move suppressions to `.editorconfig` or fix underlying issues.
- [ ] **4.6** Remove dead code: `Compile Remove`, `None Remove`, empty `Folder` includes in csproj files.

### Phase 5: Coverage Infrastructure
> **Goal:** Measure and enforce code coverage.

- [ ] **5.1** Configure coverlet with minimum coverage thresholds in `Directory.Build.props` or CI pipeline.
- [ ] **5.2** Generate coverage reports in CI (e.g., Cobertura format) and publish to a dashboard.
- [ ] **5.3** Identify uncovered critical paths and create a coverage gap backlog.
- [ ] **5.4** Set up PR gates that fail if coverage drops below threshold.

### Phase 6: Maximize Code Coverage
> **Goal:** Achieve target coverage across all source projects.

- [ ] **6.1** Prioritize coverage for `Altinn.AccessMgmt.Core` (business logic).
- [ ] **6.2** Add unit tests for `Altinn.AccessMgmt.Persistence` and `Altinn.AccessMgmt.PersistenceEF` (repository layer).
- [ ] **6.3** Add integration tests for remaining API endpoints across all API projects.
- [ ] **6.4** Add tests for `Altinn.Authorization.Host.*` libraries.
- [ ] **6.5** Improve coverage for `Altinn.Authorization.ABAC` and `Altinn.Authorization.PEP` packages.
- [ ] **6.6** Add edge case, error path, and negative tests across all layers.

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
| — | Adopt xUnit v3 as the standard | Already used by newer projects; v3 is current; self-hosting simplifies csproj |
| — | `ApiFixture` is the canonical integration test fixture | Best-designed, already shared, supports composable seeding |
| — | `EFPostgresFactory` (template cloning) over Yuniql | Faster, EF-native, already proven in newer tests |
| — | `TestUtils` is the shared test infrastructure library | Already exists, well-structured |

---

*This document will be updated as each phase progresses.*
