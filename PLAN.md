# Test Infrastructure Overhaul — Implementation Plan

## Working Instructions

You are executing a pre-written implementation plan. Follow these rules to
work effectively without wasting context:

**Context budget discipline**
- Before reading any file, state what question you need it to answer.
  If you already know the answer from PLAN.md or a previous read, skip the read.
- Read only the lines you need. Use startLine/endLine when the target is a
  known method or class — do not read whole files to find a 20-line function.
- Do not re-read a file you already read in this session unless you have a
  specific reason to believe it changed.
- Do not print file contents back to the user. Apply edits silently with tools.

**Execution discipline**
- Execute exactly the step I name. Do not look ahead or do partial work on
  the next step.
- If a step says "one class at a time", do one class, then stop and report.
- If you hit an ambiguity the plan does not resolve, ask one focused question
  before proceeding — do not guess and do not do extra reconnaissance.
- After every file edit, state in one line what you changed and why.

**Verification**
- Run `dotnet build` after each logical change.
- Report the result as: ✅ Build clean  or  ❌ Build failed — <error summary>
- Do not proceed past a build failure without fixing it first.

**Reporting**
- End each step with a three-line summary:
    Files changed: <count and names>
    Build: ✅ / ❌
    Ready for: Step N+1

**Known hard spots**
- Step 3 is the most context-intensive step. If the chat starts to lose track
  mid-step, stop, start a new chat, and resume with: "please migrate the next
  class in Step 3 — the remaining classes are X, Y, Z."
- `ClientDelegationControllerTest.cs` (Enduser.Api.Tests) has two
  `EnsureSeedOnce` calls in the same file (lines ~1162 and ~1426) belonging to
  different nested classes. Both need `EnsureSeedOnce<TKey>` with the correct
  inner class name as `TKey`. Pay attention when touching this file in Step 1.
- All step completions must be committed independently on branch
  `feature/2842_Optimize_Test_Infrastructure_and_Performance`. One commit per
  step so the history is readable and any step can be reverted cleanly.



## What We Are Doing and Why

The current test infrastructure has three overlapping fixture systems created ad-hoc by different developers:

| Fixture | Location | Problem |
|---|---|---|
| `WebApplicationFixture` | `AccessMgmt.Tests` | Calls `PostgresServer.NewEFDatabase()` (runs EF migrations) in `ConfigureWebHost`, which is invoked on every `WithWebHostBuilder` call — i.e., **once per test method**. A full scenario host is created and thrown away for every single test. |
| `PostgresFixture` | `AccessMgmt.Tests` | Legacy. Uses Yuniql migrations, partially duplicates `EFPostgresFactory`. Only used in a handful of places. |
| `ApiFixture` | `TestUtils` | The correct approach. Uses TestContainers, migrates once to a template DB, clones per fixture. But each test class gets its own `IClassFixture<ApiFixture>` — meaning N test classes = N DB clones + N web hosts. |
| `CustomWebApplicationFactory<TStartup>` | `AccessMgmt.Tests` | Lightweight but generic, redundant now that `ApiFixture` exists. |

**Root cause of slowness**: `WebApplicationFixture.ConfigureHostBuilderWithScenarios()` calls `WithWebHostBuilder()` which triggers a full `WebApplicationFactory` rebuild (including DB migration) for every test. This is the single biggest cost driver.

**Target state**: One fixture system (`ApiFixture`-based), shared across test classes via `ICollectionFixture`, with `EFPostgresFactory`'s template-clone strategy as the only DB provisioning path.

---

## Rules

**Hard rules — never break these:**
- Do not change production code (`src/apps/.../src`, `src/libs`, `src/pkgs`). Only test projects and `TestUtils` are in scope.
- Each step must leave `dotnet build` clean and `dotnet test` with zero new failures before moving on.

**Everything else is up for change.** If a fixture, pattern, file, class, method name, config value, or convention is in the way — replace it. The goal is a correct, fast, maintainable test infrastructure. Sunk cost is not a reason to keep anything.

---

## Scope

This plan covers **every test project in the solution**:

| Project | Path |
|---|---|
| `AccessMgmt.Tests` | `src/apps/Altinn.AccessManagement/test/` |
| `Altinn.AccessManagement.Enduser.Api.Tests` | `src/apps/Altinn.AccessManagement/test/` |
| `Altinn.AccessManagement.ServiceOwner.Api.Tests` | `src/apps/Altinn.AccessManagement/test/` |
| `Altinn.AccessManagement.Api.Tests` | `src/apps/Altinn.AccessManagement/test/` |
| `Altinn.AccessMgmt.PersistenceEF.Tests` | `src/apps/Altinn.AccessManagement/test/` |
| `Altinn.AccessMgmt.Core.Tests` | `src/apps/Altinn.AccessManagement/test/` |
| `Altinn.Authorization.Tests` | `src/apps/Altinn.Authorization/test/` |
| `Altinn.Authorization.Integration.Tests` | `src/libs/Altinn.Authorization.Integration/test/` |
| `Altinn.Authorization.Host.Lease.Tests` | `src/libs/Altinn.Authorization.Host/test/` |
| `Altinn.Authorization.PEP.Tests` | `src/pkgs/Altinn.Authorization.PEP/test/` |

**Not in scope**: `Altinn.Authorization.ABAC.Tests` — contains no source test files, nothing to do.

**Migration boundary**: The `ApiFixture`/`WebApplicationFixture` fixture migration (Steps 2–5) applies only to `AccessMgmt.Tests`. `Altinn.Authorization.Tests` has its own `CustomWebApplicationFactory` tied to the `Altinn.Authorization` app stack — it is out of scope for fixture migration but **in scope** for traits (Step 6) and `xunit.runner.json` (Step 7).

---

## Step-by-Step Implementation

### Step 0 — Measure baseline (do this before any code changes)

Record current timings so the result at Step 9 is meaningful. Run from the repo root:

```powershell
# Per-project timing — run each separately so Docker startup is not shared
dotnet test src/apps/Altinn.AccessManagement/test/AccessMgmt.Tests `
    --logger "trx;LogFileName=baseline_AccessMgmt.trx" `
    --logger "console;verbosity=normal" | Tee-Object baseline_AccessMgmt.txt

dotnet test src/apps/Altinn.AccessManagement/test/Altinn.AccessManagement.Enduser.Api.Tests `
    --logger "trx;LogFileName=baseline_Enduser.trx" `
    --logger "console;verbosity=normal" | Tee-Object baseline_Enduser.txt

dotnet test src/apps/Altinn.AccessManagement/test/Altinn.AccessManagement.ServiceOwner.Api.Tests `
    --logger "trx;LogFileName=baseline_ServiceOwner.trx" `
    --logger "console;verbosity=normal" | Tee-Object baseline_ServiceOwner.txt
```

**Record these numbers** (copy from the console output into a comment at the top of this file or a scratch note):
- Total elapsed time per project
- Number of tests passed/failed
- The 5 slowest individual tests per project (visible in the `trx` file via any XML viewer or VS Test Explorer)

**Do not proceed to Step 1 until these numbers are written down.** They are the only way to validate that the work succeeded.

**Acceptance**: Baseline timings recorded. All tests pass at baseline.

---

### Step 1 — Overhaul `ApiFixture` public API

Three changes to `ApiFixture` in one commit, all in `TestUtils/Fixtures/ApiFixture.cs`:

**1a — Fix `ConfiureServices` typo**

Rename to `ConfigureServices`. Search all test projects for `ConfiureServices(` and update every call site.

**1b — Redesign `EnsureSeedOnce` to support shared fixtures**

The current implementation uses a single `int _seedonce` gate. This means when multiple test classes share one `ApiFixture` via `ICollectionFixture`, only the first class to call `EnsureSeedOnce` actually seeds — all others are silently dropped. Replace with a `ConcurrentDictionary<Type, bool>` keyed on a type parameter supplied by the caller:

```csharp
// Replace the existing _seedonce field and EnsureSeedOnce method with:
private readonly ConcurrentDictionary<Type, bool> _seededKeys = new();

// For IClassFixture usage — caller passes its own type as the key
public void EnsureSeedOnce<TKey>(params Action<AppDbContext>[] configureDb)
{
    if (_seededKeys.TryAdd(typeof(TKey), true))
    {
        var audit = new AuditValues(SystemEntityConstants.StaticDataIngest);
        using var scope = Services.CreateEFScope(audit);
        using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        foreach (var configure in configureDb)
            configure(db);
    }
}
```

Update all existing `EnsureSeedOnce(...)` call sites to `EnsureSeedOnce<TCallerClass>(...)` where `TCallerClass` is the test class making the call. This is a mechanical change — the seeds themselves do not change.

**1c — Add XML `<summary>` docs**

Add or update `<summary>` on `EnsureSeedOnce<TKey>`, `ConfigureServices`, `BuildConfiguration`, and `QueryDb` describing when to use each and any ordering constraints (all of `ConfigureServices`, `EnsureSeedOnce`, and `BuildConfiguration` must be called before the fixture's web host is first accessed).

**Acceptance**: `ConfiureServices` does not appear anywhere. All `EnsureSeedOnce` call sites use the generic overload. The four public methods each have a `<summary>` doc.

---

### Step 2 — Audit all `AccessMgmt.Tests` classes that use legacy fixtures

Before migrating anything, read every test file in `AccessMgmt.Tests` that uses `WebApplicationFixture`, `CustomWebApplicationFactory`, or `PostgresFixture` — this includes controller tests, service tests, hosted-service tests, and any other test class. Answer these questions for each:

1. Does each test method call `ConfigureHostBuilderWithScenarios` with **different** scenarios, or the same ones?
2. Which mock clients does the class need — `IPartiesClient`, `IProfileClient`, `IResourceRegistryClient`, `IPolicyRetrievalPoint`, others? Do they vary per test?
3. Does any test write to the database?

**Record the answers as a structured comment** at the top of each file using this exact format before writing any migration code:

```csharp
// Audit:
//   Pattern: A-isolated | A-shared | B
//   Mocks: IPartiesClient, IProfileClient   (list only what is actually used)
//   Writes: Yes | No
//   Notes: <anything unusual>
```

This is reconnaissance only — no code changes yet. The migration strategy in Step 3 depends entirely on what you find here.

**Expected reality for `AccessMgmt.Tests`**: `ApiFixture`'s default registrations are `PermitPdpMock` and `PublicSigningKeyProviderMock`. Most `AccessMgmt.Tests` classes also need `IPartiesClient`, `IProfileClient`, `IResourceRegistryClient`, and `IPolicyRetrievalPoint`. These are not in the defaults, so the vast majority of classes will be **Pattern A-isolated**. Pattern A-shared will apply to very few classes here. That is expected and fine — the primary gain for `AccessMgmt.Tests` is eliminating per-test host rebuilds, not fixture sharing.

**Acceptance**: Every test class in `AccessMgmt.Tests` that uses a legacy fixture has an `// Audit:` comment. No migration code has been written yet.

---

### Step 3 — Migrate all `AccessMgmt.Tests` legacy fixture users to `ApiFixture`

Work through every class annotated in Step 2. **Migrate one class at a time**: make the change, run `dotnet build`, run `dotnet test src/apps/Altinn.AccessManagement/test/AccessMgmt.Tests`, confirm zero new failures, then move to the next class. Do not batch multiple classes into one change.

Apply the pattern assigned during the audit.

There are three patterns. Assign each class to the appropriate one based on the audit in Step 2.

**Pattern A-isolated** — needs custom mock services (calls `ConfigureServices`):

Gives the class its own `IClassFixture<ApiFixture>`. Service registrations happen in the constructor and are locked before the host starts. This class will **not** join a shared collection — custom service config makes sharing impossible because the web host is shared and its DI container is fixed at build time.

```csharp
public class MyControllerTest : IClassFixture<ApiFixture>
{
    private readonly HttpClient _client;

    public MyControllerTest(ApiFixture fixture)
    {
        fixture.ConfigureServices(services =>
        {
            services.AddSingleton<IPartiesClient>(new PartiesClientMock(PersonSeeds.Paula));
        });
        fixture.EnsureSeedOnce<MyControllerTest>(db => { /* seed rows this class needs */ });
        _client = fixture.BuildConfiguration(
            c => c.DefaultRequestHeaders.Authorization = TestTokenGenerator.PersonToken(PersonSeeds.Paula));
    }

    [Fact]
    public async Task SomeTest() => Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync(...)).StatusCode);
}
```

**Pattern A-shared** — no custom mock services, works with `ApiFixture`'s default registrations:

Eliminate the `ConfigureServices` call entirely. Use `EnsureSeedOnce<TKey>` for additive seeds. This class **can** join the `"Default"` shared collection in Step 5.

```csharp
// No ConfigureServices call — relies on mocks already registered by ApiFixture
public class MyControllerTest : IClassFixture<ApiFixture>
{
    private readonly HttpClient _client;

    public MyControllerTest(ApiFixture fixture)
    {
        fixture.EnsureSeedOnce<MyControllerTest>(db => { /* seed rows this class needs */ });
        _client = fixture.BuildConfiguration(
            c => c.DefaultRequestHeaders.Authorization = TestTokenGenerator.PersonToken(PersonSeeds.Paula));
    }

    [Fact]
    public async Task SomeTest() => Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync(...)).StatusCode);
}
```

**Pattern B** — varying mock state per test (different scenarios per `[Fact]`):

Split into inner classes, one per distinct mock configuration. Each inner class uses `IClassFixture<ApiFixture>` and gets its own fixture. xUnit discovers nested test classes automatically.

```csharp
public class MyControllerTest
{
    public class AsAdmin : IClassFixture<ApiFixture>
    {
        public AsAdmin(ApiFixture fixture) { fixture.ConfigureServices(services => { /* admin mocks */ }); }
        [Fact] public async Task CanAccessAdminEndpoint() { ... }
    }

    public class AsUser : IClassFixture<ApiFixture>
    {
        public AsUser(ApiFixture fixture) { fixture.ConfigureServices(services => { /* user mocks */ }); }
        [Fact] public async Task CannotAccessAdminEndpoint() { ... }
    }
}
```

**Rules that apply to all patterns**:
- `ConfigureServices`, `EnsureSeedOnce`, and `BuildConfiguration` must all be called from the constructor, never from inside a `[Fact]`. By the time a test method runs, the host is already built and those calls have no effect.
- Write-heavy classes that mutate DB rows other classes depend on must use `IClassFixture<ApiFixture>` (Pattern A-isolated) regardless of mock needs.

**Acceptance**: No test class references `WebApplicationFixture`, `ConfigureHostBuilderWithScenarios`, `MockContext`, or `Scenario` delegates.

---

### Step 4 — Delete legacy fixtures and dead code

Delete in this order (each deletion must leave the build green):

1. `AccessMgmt.Tests/Fixtures/WebApplicationFixture.cs` — verify zero references first
2. `AccessMgmt.Tests/Fixtures/PostgresFixture.cs` — verify zero references first
3. `AccessMgmt.Tests/CustomWebApplicationFactory.cs` — verify zero references first
4. `AccessMgmt.Tests/Templates/DatabaseTestTemplate.cs` — verify zero references first
5. `AccessMgmt.Tests/Templates/ControllerTestTemplate.cs` — verify zero references first
6. `AccessMgmt.Tests/Fixtures/PostgresServer.cs` — verify zero references first
7. **Check `AccessMgmt.Tests/Scenarios/`** — `Scenario.cs`, `TokenScenario.cs`, `DelegationScenarios.cs` were only used by `WebApplicationFixture`-based tests. Run Find All References on each type. If zero references remain, delete the files. If any test class still references a scenario type, that class was missed in Step 3 — go back and fix it.
8. **Check `AccessMgmt.Tests/Seeds/`** — seed classes that were only used to populate `MockContext` may also be dead. Run Find All References on each seed class. Delete any with zero references outside `Scenarios/`.
9. **Check `AccessMgmt.Tests/Contexts/`** and **`AccessMgmt.Tests/Mocks/`** — mock implementations previously injected via `MockContext` may now be unused. Run Find All References on each mock class. Delete any with zero references.

**How to verify zero references**: Use "Find All References" on each type before deleting. If any references remain, go back and fix them before deleting.

---

### Step 5 — Share `ApiFixture` across test classes using `ICollectionFixture`

At this point every integration test class uses `IClassFixture<ApiFixture>`. The next step is reducing the number of fixture instances by grouping classes that can safely share one host and one DB clone.

**Rule for sharing**: A test class can join a shared collection if and only if:
- It does **not** call `ConfigureServices` — service registrations are baked into the shared host at build time; any class needing custom mocks must own its fixture and is ineligible
- It seeds only via `EnsureSeedOnce<TKey>` using its own type as `TKey` — additive inserts only, no deletes or updates to rows other classes read
- Its tests do not mutate rows that other classes depend on reading

Classes migrated as **Pattern A-isolated** or **Pattern B** in Step 3 are ineligible by definition. Only **Pattern A-shared** classes belong here.

**How to implement**:

1. Create a `Collections.cs` file in each test project's root:

```csharp
[CollectionDefinition("Default")]
public class DefaultCollection : ICollectionFixture<ApiFixture> { }
```

2. Add `[Collection("Default")]` to every test class that meets the sharing rule above.

3. Test classes that do **not** meet the rule keep `IClassFixture<ApiFixture>` and get no `[Collection]` attribute — xUnit will still run them, each with their own fixture instance.

**Naming convention**:
- `"Default"` — shared `ApiFixture`, safe for read-heavy and `EnsureSeedOnce`-seeded tests
- No attribute / `IClassFixture` only — write-heavy or uniquely-configured tests, isolated by default

**Files to create**:
- `AccessMgmt.Tests/Collections.cs`
- `Altinn.AccessManagement.Enduser.Api.Tests/Collections.cs`
- `Altinn.AccessManagement.ServiceOwner.Api.Tests/Collections.cs`

**Acceptance**: Total `ApiFixture` instances created during a full test run equals the number of distinct collections plus the number of unshared `IClassFixture` classes — visibly fewer than the current count of test classes.

---

### Step 6 — Add `[Trait]` categorization

Add traits to **all** test classes in all test projects. Apply at class level, not method level.

**Trait values**:
- `[Trait("Category", "Unit")]` — no DB, no web server, no containers (model tests, extension tests, constant tests)
- `[Trait("Category", "Integration")]` — uses `ApiFixture`, hits web server and DB

**Usage**:
```
dotnet test --filter "Category=Unit"        # fast, no infrastructure
dotnet test --filter "Category=Integration" # requires Docker
```

**Files to update** — every test class in:
- `AccessMgmt.Tests`
- `Altinn.AccessManagement.Enduser.Api.Tests`
- `Altinn.AccessManagement.ServiceOwner.Api.Tests`
- `Altinn.AccessManagement.Api.Tests` — `RequestEndToEndTest` and `ProblemsValidation` are both `Integration`
- `Altinn.AccessMgmt.PersistenceEF.Tests` — all `Unit`
- `Altinn.AccessMgmt.Core.Tests`
- `Altinn.Authorization.Tests` — mixed; classes that construct a `CustomWebApplicationFactory` are `Integration`, pure in-memory classes (e.g. `DelegationHelperTest`, `EventMapperServiceTest`, `Xacml30ConformanceTests`) are `Unit`; read each file to determine which applies before adding the trait
- `Altinn.Authorization.Integration.Tests` — all `Integration` (every class uses `PlatformFixture`)
- `Altinn.Authorization.Host.Lease.Tests` — read each file; if it spins up real infrastructure (Azure Storage, containers) it is `Integration`, otherwise `Unit`
- `Altinn.Authorization.PEP.Tests` — all `Unit`

---

### Step 7 — Add `xunit.runner.json` to projects that are missing it

**Projects currently without `xunit.runner.json`** (verify each before adding — some may already have one):
- `Altinn.AccessManagement.ServiceOwner.Api.Tests`
- `Altinn.AccessManagement.Api.Tests`
- `Altinn.Authorization.Tests`
- `Altinn.Authorization.Integration.Tests`
- `Altinn.Authorization.Host.Lease.Tests`
- `Altinn.Authorization.PEP.Tests`

**Template** (copy from `AccessMgmt.Tests/xunit.runner.json`):
```json
{
  "$schema": "https://xunit.net/schema/current/xunit.runner.schema.json",
  "parallelizeAssembly": true,
  "parallelizeTestCollections": true,
  "maxParallelThreads": 4,
  "methodDisplay": "method",
  "methodDisplayOptions": "all",
  "diagnosticMessages": false,
  "internalDiagnosticMessages": false
}
```

Add to the `.csproj`:
```xml
<ItemGroup>
  <Content Include="xunit.runner.json" CopyToOutputDirectory="Always" />
</ItemGroup>
```

---

### Step 8 — Create `TESTING.md` and update `README.md`

**`TESTING.md`** — create at repo root. Target audience: developer new to the project.

Required sections:
1. **Running tests** — `dotnet test`, filter by `Category=Unit` vs `Category=Integration`
2. **Fixture selection** — decision tree: no infrastructure → no fixture; needs web server + DB → `ApiFixture`; needs write isolation → Pattern A-isolated; can share → Pattern A-shared + collection
3. **Adding a new test class** — step-by-step with code example showing `EnsureSeedOnce<TKey>`, `ConfigureServices`, `BuildConfiguration`
4. **Collection membership rules** — the three conditions from Step 5 stated plainly
5. **Why `WebApplicationFixture` is gone** — one paragraph

**`README.md`** — add a `## Testing` section (or update if one exists) that links to `TESTING.md` and shows the two most useful commands:
```
dotnet test --filter "Category=Unit"        # fast, no Docker required
dotnet test --filter "Category=Integration" # requires Docker
```

---

### Step 9 — Measure result and record improvement

Repeat the exact same commands from Step 0:

```powershell
dotnet test src/apps/Altinn.AccessManagement/test/AccessMgmt.Tests `
    --logger "trx;LogFileName=after_AccessMgmt.trx" `
    --logger "console;verbosity=normal" | Tee-Object after_AccessMgmt.txt

dotnet test src/apps/Altinn.AccessManagement/test/Altinn.AccessManagement.Enduser.Api.Tests `
    --logger "trx;LogFileName=after_Enduser.trx" `
    --logger "console;verbosity=normal" | Tee-Object after_Enduser.txt

dotnet test src/apps/Altinn.AccessManagement/test/Altinn.AccessManagement.ServiceOwner.Api.Tests `
    --logger "trx;LogFileName=after_ServiceOwner.trx" `
    --logger "console;verbosity=normal" | Tee-Object after_ServiceOwner.txt
```

Compare against Step 0 numbers. Record the delta (time saved, % reduction) in a comment in the PR description or in `TESTING.md` under a `## Performance` section.

**Acceptance**: All tests still pass. Total elapsed time is measurably lower than baseline. If the reduction is less than expected, investigate before closing the work.

---

## Files Inventory

### Delete (after migration)
```
src/apps/Altinn.AccessManagement/test/AccessMgmt.Tests/Fixtures/WebApplicationFixture.cs
src/apps/Altinn.AccessManagement/test/AccessMgmt.Tests/Fixtures/PostgresFixture.cs
src/apps/Altinn.AccessManagement/test/AccessMgmt.Tests/Fixtures/PostgresServer.cs
src/apps/Altinn.AccessManagement/test/AccessMgmt.Tests/CustomWebApplicationFactory.cs
src/apps/Altinn.AccessManagement/test/AccessMgmt.Tests/Templates/DatabaseTestTemplate.cs
src/apps/Altinn.AccessManagement/test/AccessMgmt.Tests/Templates/ControllerTestTemplate.cs
```

### Create
```
src/apps/Altinn.AccessManagement/test/AccessMgmt.Tests/Collections.cs
src/apps/Altinn.AccessManagement/test/Altinn.AccessManagement.Enduser.Api.Tests/Collections.cs
src/apps/Altinn.AccessManagement/test/Altinn.AccessManagement.ServiceOwner.Api.Tests/Collections.cs
src/apps/Altinn.AccessManagement/test/Altinn.AccessManagement.ServiceOwner.Api.Tests/xunit.runner.json
src/apps/Altinn.AccessManagement/test/Altinn.AccessManagement.Api.Tests/xunit.runner.json
src/apps/Altinn.Authorization/test/Altinn.Authorization.Tests/xunit.runner.json
src/libs/Altinn.Authorization.Integration/test/Altinn.Authorization.Integration.Tests/xunit.runner.json
src/libs/Altinn.Authorization.Host/test/Altinn.Authorization.Host.Lease.Tests/xunit.runner.json
src/pkgs/Altinn.Authorization.PEP/test/Altinn.Authorization.PEP.Tests/xunit.runner.json
TESTING.md
```

### Update
```
README.md — add ## Testing section linking to TESTING.md
```

### Modify
```
src/apps/Altinn.AccessManagement/test/Altinn.AccessManagement.TestUtils/Fixtures/ApiFixture.cs
  - Rename ConfiureServices → ConfigureServices (Step 1)
  - Redesign EnsureSeedOnce to keyed EnsureSeedOnce<TKey> (Step 1)
src/apps/Altinn.AccessManagement/test/AccessMgmt.Tests/**/*Test*.cs
  - Replace WebApplicationFixture usage with ApiFixture (Steps 2-3)
  - Add [Collection] and [Trait] attributes (Steps 5-6)
src/apps/Altinn.AccessManagement/test/Altinn.AccessManagement.Enduser.Api.Tests/**/*Test*.cs
  - Add [Collection("Default")] and [Trait] attributes (Steps 5-6)
src/apps/Altinn.AccessManagement/test/Altinn.AccessManagement.ServiceOwner.Api.Tests/**/*Test*.cs
  - Add [Collection("Default")] and [Trait] attributes (Steps 5-6)
src/apps/Altinn.AccessManagement/test/Altinn.AccessMgmt.PersistenceEF.Tests/**/*Test*.cs
  - Add [Trait("Category", "Unit")] (Step 6)
src/apps/Altinn.AccessManagement/test/Altinn.AccessManagement.Api.Tests/**/*Test*.cs
  - Add [Trait("Category", "Integration")] (Step 6)
src/apps/Altinn.Authorization/test/Altinn.Authorization.Tests/**/*.cs (test classes only)
  - Add [Trait("Category", "Unit"|"Integration")] per class (Step 6)
src/libs/Altinn.Authorization.Integration/test/Altinn.Authorization.Integration.Tests/**/*.cs
  - Add [Trait("Category", "Integration")] (Step 6)
src/libs/Altinn.Authorization.Host/test/Altinn.Authorization.Host.Lease.Tests/**/*.cs
  - Add [Trait] per class after reading each file (Step 6)
src/pkgs/Altinn.Authorization.PEP/test/Altinn.Authorization.PEP.Tests/**/*.cs
  - Add [Trait("Category", "Unit")] (Step 6)
```

---

## Execution Order

Work through steps **0 → 9 in order**. After each step:
1. Run `dotnet build` — must be clean
2. Run `dotnet test` on the affected project — zero new failures allowed
3. Commit the step independently

Do **not** batch steps together.

---

## What Not To Do

- Do not add BenchmarkDotNet or performance-gating tests — measuring with `dotnet test` timings is sufficient
- Do not convert PostgreSQL tests to in-memory EF — loses fidelity on DB-specific behaviour
- Do not introduce new NuGet packages
- Do not change production code (the hard rule from the top)
