# Fixtures

A *fixture* is the xUnit-managed object whose lifetime spans one test class
(`IClassFixture<T>`) or one collection (`ICollectionFixture<T>`). Integration
tests use fixtures to host a `WebApplicationFactory<Program>` and to provision
the backing PostgreSQL database.

This repo has **three** fixtures you should know about. Pick the simplest one
that works for what you're testing.

---

## 1. `ApiFixture` — canonical integration fixture

**Location:** `Altinn.AccessManagement.TestUtils/Fixtures/ApiFixture.cs`
**Use for:** All new AccessManagement integration tests.
**Database:** EF Core migrations, template-cloned per fixture via
`EFPostgresFactory`.

`ApiFixture` extends `WebApplicationFactory<Program>` and implements
`IAsyncLifetime`. It is composable — register configuration and service
overrides in the test-class constructor, not in test methods.

### Basic usage

```csharp
public class MyControllerTest : IClassFixture<ApiFixture>
{
    private readonly HttpClient _client;

    public MyControllerTest(ApiFixture fixture)
    {
        // Register overrides BEFORE requesting a client.
        fixture.ConfigureServices(services =>
        {
            services.AddSingleton<IFoo, FooMock>();
        });

        _client = fixture.CreateTestClient(c =>
        {
            c.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", TestTokens.ForUser(1337));
        });
    }
}
```

### One-shot seeding

```csharp
fixture.EnsureSeedOnce<MyControllerTest>(async db =>
{
    db.Parties.Add(new Party { ... });
    await db.SaveChangesAsync();
});
```

The callback runs exactly once per fixture lifetime even when the fixture is
shared across classes via `ICollectionFixture<ApiFixture>`.

#### Seed-data contract

`ApiFixture` is **not** an empty database — it provisions a baseline set of
entities, parties and assignments (see `TestDataSeeds` / `TestEntities` /
`TestData` in `Altinn.AccessManagement.TestUtils`). Your test code should:

- **Assert against owned data, not global counts.** Use scoped assertions
  (`Assert.Contains` / `Assert.Empty` on a filtered set), never "exactly N rows
  in the table" — the baseline and other classes' `EnsureSeedOnce` data coexist
  in a shared DB.
- **Use disjoint IDs.** When seeding your own rows, use IDs that cannot collide
  with the baseline or another class sharing the fixture; a primary-key clash
  surfaces as a seed-time failure, not a clean assertion error.
- **Reuse the named baseline actors** (`TestData.MalinEmilie`,
  `TestData.JinxArcane`, `TestEntities.MainUnitKarlstad`, …) rather than
  inventing parties, so the register/profile/roles mocks already resolve them.

### Why you configure in the constructor only

Once `CreateClient()` / `CreateTestClient()` is called, the underlying
`IHost` is built and frozen. Any `ConfigureServices` registered after that
point is **silently ignored**. If you need a different configuration for a
subset of tests, put those tests in a dedicated class with its own fixture.

---

## 2. `AuthorizationApiFixture` — Authorization app fixture

**Location:** `Altinn.Authorization.Tests/Fixtures/AuthorizationApiFixture.cs`
**Use for:** All `Altinn.Authorization.Tests` controller tests.
**Database:** None — the Authorization app uses mocks for its data stores.

Pre-registers the full common mock graph for Authorization (PolicyRetrievalPoint,
PolicyRepository, DelegationMetadataRepository, signing key provider, events
queue, etc.) so test classes don't have to duplicate that wiring.

```csharp
public class DecisionControllerTest : IClassFixture<AuthorizationApiFixture>
{
    private readonly HttpClient _client;

    public DecisionControllerTest(AuthorizationApiFixture fixture)
    {
        // Add per-class overrides before building the client.
        fixture.ConfigureServices(s => s.AddSingleton<IContextHandler, MyHandler>());
        _client = fixture.CreateClient();
    }
}
```

For per-test variation, call `WithWebHostBuilder` — the common mocks are
inherited automatically:

```csharp
var client = fixture.WithWebHostBuilder(b =>
    b.ConfigureTestServices(s => s.AddSingleton<IFeatureManager>(customMock)))
    .CreateClient();
```

**Feature flags only apply through the injected mock.** A flag-gated path (for
example the access-package enrichment in `ContextHandler`, gated by
`SystemUserAccessPackageAuthorization` / `UserAccessPackageAuthorization`) is
only enabled when you build the client with the configured `IFeatureManager`
mock (via `WithWebHostBuilder` / a `GetTestClient(featureManager: …)` helper) —
the default `BuildClient()` does not pick up the per-class `featureManageMock`
setups. This is a subtle trap for **negative** decision tests: a flag-gated
enrichment that is silently *off* yields the same `NotApplicable` you expect for
"no access", so the test passes for the wrong reason. Run such tests through the
flag-enabled client so the decision is actually exercised.

---

## 3. `LegacyApiFixture` — legacy-schema tests

**Location:** `AccessMgmt.Tests/Fixtures/LegacyApiFixture.cs`
**Use for:** Tests backed by the Dapper / raw-Npgsql repositories on the
`accessmanagement`, `delegation` and `consent` schemas.
**Database:** EF Core (the `dbo` and `consent` schemas) **and** the Yuniql
`accessmanagement` / `delegation` schemas, side-by-side.

This fixture exists as an explicit bridge. New tests should **not** use it —
prefer `ApiFixture`. The expected outcome is that the last `LegacyApiFixture`
consumers get rewritten on EF seed data over time and the fixture is retired.

---

## `EFPostgresFactory` — how the DB is provisioned

Under the hood, `ApiFixture` delegates database provisioning to
`EFPostgresFactory`, a thin wrapper over the shared
[`PostgresTestEngine`](../../src/testing/PostgresTestEngine.cs) (linked into test
assemblies that set `IncludePostgresTestEngine`, the same mechanism as the
`[UnitTest]`/`[IntegrationTest]` markers). The strategy:

1. A **single** PostgreSQL container is shared across every fixture in the
   test run (reference counted).
2. EF Core migrations + static data are applied **once** to a **template**
   database.
3. Every fixture gets a fresh database via `CREATE DATABASE <name> WITH
   TEMPLATE <template>`. Cloning is ~100–500 ms, vs ~10+ seconds for
   re-running migrations.

For tests that exercise EF services or repositories directly (no web host),
`EfDatabaseFixture` exposes the same template-cloned database as a plain
`IClassFixture<EfDatabaseFixture>` — build an `AppDbContext` from `Db`
(`Db.Admin.ToString()`; `PostgresDatabase` exposes separate `Admin`/`User`
connection-string builders). It reuses the single shared container, so no second
PostgreSQL container is needed.

### Graceful skip when no container runtime is available

When no Docker / Podman socket is reachable, `PostgresTestEngine` records a
`SkipReason` instead of throwing — a skip raised from a fixture's
`InitializeAsync` surfaces as a fixture-init *failure*, not a skip, in xUnit v3.
Fixtures convert that reason to a per-test skip: `AuthorizationDbFixture` exposes
`SkipReason` and its tests call `Assert.SkipWhen(...)`. `ApiFixture` /
`EFPostgresFactory` still surface a missing runtime as a failure (converting its
many test classes to the per-test-skip pattern is tracked separately); in CI a
runtime is always present.

## Other fixtures

- **`AuthorizationDbFixture`** (`Altinn.Authorization.Tests`) — provides a
  PostgreSQL database with the Authorization `delegation` schema for the
  delegation-metadata repository tests. Backed by the shared `PostgresTestEngine`
  (the EF Core migrations are applied once into a template; each test class gets a
  clone). (`AuthorizationApiFixture` itself is mock-backed and needs no container.)
- **`PlatformFixture`** (`Altinn.Authorization.Integration.Tests`) — wires up the
  platform-integration clients against the **live** platform; its tests
  `Assert.Skip(...)` when credentials/config are missing, so they don't run in a
  plain local or CI build.

---

## Decision table

| Question | Answer |
|---|---|
| New AccessManagement integration test? | `ApiFixture` |
| New Authorization controller test? | `AuthorizationApiFixture` |
| Pure logic, no DI, no DB? | **No fixture.** Construct the SUT directly with Moq dependencies. |
| Repository / EF Core test? | `ApiFixture` or a minimal fixture built on `EFPostgresFactory`. |
| You see `LegacyApiFixture` in older tests? | Leave it as-is unless you're rewriting that file. Don't introduce new consumers. |

## Next: [MOCKS_AND_TESTUTILS.md](MOCKS_AND_TESTUTILS.md)
