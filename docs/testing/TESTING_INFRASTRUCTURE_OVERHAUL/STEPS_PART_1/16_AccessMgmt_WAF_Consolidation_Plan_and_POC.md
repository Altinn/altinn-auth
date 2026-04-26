# Step 16 — AccessMgmt.Tests WAF Consolidation (Phase 2.2–2.3): Plan + POC

## Goal

Begin the consolidation of `AccessMgmt.Tests` onto the canonical `ApiFixture`
pattern (from `Altinn.AccessManagement.TestUtils`), retiring
`CustomWebApplicationFactory<TController>` and eventually
`WebApplicationFixture`/`PostgresFixture`/`PostgresServer`.

Because this is the largest single refactor in the overhaul plan (15 consumer
classes across two distinct factory patterns, plus `AcceptanceCriteriaComposer`
and `Scenarios` infrastructure), this step delivers:

1. A comprehensive **migration plan** with a per-file audit table.
2. A validated **proof-of-concept migration** of one representative consumer
   (`ResourceControllerTest`).
3. A documented **migration recipe** so follow-up steps can execute
   file-by-file predictably.

## Current State (audit)

AccessMgmt.Tests has **two** integration-test factory patterns:

### Group A — `CustomWebApplicationFactory<TController>` (7 classes)

No DB; `appsettings.test.json` + per-class mock wiring via
`_factory.WithWebHostBuilder(...)` in a local `GetTestClient()` helper.

| # | Class | GetTestClient shape | Notes |
|---|---|---|---|
| 1 | `ResourceControllerTest` | Constructor, static mock set | **POC target** — audited clean. No DB writes. |
| 2 | `PolicyInformationPointControllerTest` | Per-test, optional `IDelegationMetadataRepository` override | Mocks repo; no DB writes. |
| 3 | `DelegationsControllerTest` | Via `SetupUtils.GetTestClient` | ~10+ singletons; shared helper. |
| 4 | `Altinn2RightsControllerTest` | Per-class | Uses `Tests.Mocks.Altinn2RightsClientMock` (JSON-backed, not consolidatable). |
| 5 | `MaskinportenSchemaControllerTest` | Per-class | — |
| 6 | `RightsInternalControllerTest` | Per-test call; two tests pass `WithPDPMock` override | **Complex** — audit suggests splitting into two inner classes `WithDefaultMocks` / `WithPdpMock`. ~1600 lines. |
| 7 | `AppsInstanceDelegationControllerTest` | Per-test with `platformToken` param | — |

### Group B — `WebApplicationFixture` (7 classes, scenario-based)

Uses real `PostgresServer` via `IClassFixture<WebApplicationFixture>`;
scenario composition via `fixture.ConfigureHostBuilderWithScenarios(...)`.

| # | Class | State |
|---|---|---|
| 1 | `HealthCheckTests` | Trivial — only needs `/health` + `/alive`. Easy migration. |
| 2 | `PartyControllerTests` | Thread-safe static factory pattern. Medium. |
| 3 | `ConsentControllerTestBFF` | Scenario-based (DelegationScenarios). |
| 4 | `ConsentControllerTestEnterprise` | Scenario-based. |
| 5 | `ConsentControllerTest` (MaskinPorten) | Scenario-based. |
| 6 | `V2MaskinportenSchemaControllerTest` | **All `[Skip]`ped** — migration unblocks these. |
| 7 | `V2RightsInternalControllerTest` | **All `[Skip]`ped** — migration unblocks these. |
| 8 | `V2ResourceControllerTest` | Uses `AcceptanceCriteriaComposer`. |

### Supporting infrastructure (will follow consumer migration)

- `CustomWebApplicationFactory.cs` — delete after Group A fully migrated.
- `SetupUtils.GetTestClient(CustomWebApplicationFactory<DelegationsController>)`
  — replace with per-class `ApiFixture.ConfigureServices` or a reusable
  `AccessMgmtMocks` extension on `IServiceCollection`.
- `Fixtures/WebApplicationFixture.cs` + `Fixtures/PostgresFixture.cs`
  (`PostgresServer`) — delete after Group B fully migrated.
- `Templates/ControllerTestTemplate.cs` — rewrite against `ApiFixture`.
- `AcceptanceCriteriaComposer.cs` + `Scenarios/*.cs` — rewrite or retire.
  `AcceptanceCriteriaComposer.Test(Fixture)` is the replacement target.

## Migration Recipe (Group A)

Adopted and validated against `ResourceControllerTest`:

```csharp
// Before
public class FooControllerTest : IClassFixture<CustomWebApplicationFactory<FooController>>
{
    private readonly CustomWebApplicationFactory<FooController> _factory;
    private readonly HttpClient _client;

    public FooControllerTest(CustomWebApplicationFactory<FooController> factory)
    {
        _factory = factory;
        _client = GetTestClient();
    }

    private HttpClient GetTestClient()
    {
        return _factory.WithWebHostBuilder(b => b.ConfigureTestServices(s =>
        {
            s.AddSingleton<IFoo, FooMock>();
            s.AddSingleton<IPublicSigningKeyProvider, SigningKeyResolverMock>();
            // ...
        })).CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }
}

// After
public class FooControllerTest : IClassFixture<ApiFixture>
{
    private readonly HttpClient _client;

    public FooControllerTest(ApiFixture fixture)
    {
        // Preserve legacy appsettings overrides (Azure Storage, Cosmos, feature flags).
        fixture.WithAppsettings(b => b.AddJsonFile("appsettings.test.json", optional: false));

        fixture.ConfigureServices(services =>
        {
            services.AddSingleton<IFoo, FooMock>();

            // ApiFixture registers PublicSigningKeyProviderMock by default, but our tests
            // need SigningKeyResolverMock (loads {issuer}-org.pem certs from disk to
            // validate tokens signed by PrincipalUtil.GetAccessToken).
            services.RemoveAll<IPublicSigningKeyProvider>();
            services.AddSingleton<IPublicSigningKeyProvider, SigningKeyResolverMock>();
            // ...
        });

        _client = fixture.CreateClient(new() { AllowAutoRedirect = false });
    }
}
```

### Key migration rules

1. **Call `fixture.WithAppsettings` / `fixture.ConfigureServices` from the
   constructor only.** Both are one-shot: the host is sealed on first
   `CreateClient()`.
2. **Each Group-A class uses its own `IClassFixture<ApiFixture>`** — do not
   share via `ICollectionFixture<ApiFixture>` because each test class
   configures its DI uniquely.
3. **Remove `IPublicSigningKeyProvider` before re-adding `SigningKeyResolverMock`**
   so the issuer-cert-based resolver wins over the default test-key mock.
4. **Preserve `appsettings.test.json`** — AccessMgmt.Tests depends on its
   Azure Storage, Cosmos, and FeatureManagement keys.
5. **Avoid `[Collection("…")]` attributes** unless parallelism is actually
   harmful — `IClassFixture<ApiFixture>` already isolates per class.
6. **xUnit v3 caveat:** `ApiFixture.ConfigureServices` appends; there is no
   supported way to swap DI per-test. If a test class has two mutually
   exclusive mock configurations (e.g. `RightsInternalControllerTest`'s
   `WithPDPMock` variant), split into nested classes each with their own
   `IClassFixture<ApiFixture>`.

### Postgres overhead

`ApiFixture.InitializeAsync` calls `EFPostgresFactory.Create()`, which clones
a template Postgres database. Group-A tests mock out repositories and never
touch the DB, so the container is pure overhead for them. This is acceptable
because:

- Podman-backed `EFPostgresFactory` is already required for the rest of
  AccessMgmt.Tests (Step 12).
- Template cloning is fast (< 500 ms per DB).
- Consolidating onto one fixture eliminates the second Postgres stack
  (`PostgresServer` / Yuniql) per Phase 2.6.

## POC Result — `ResourceControllerTest`

Migrated in this step. All 7 tests pass under Podman:

```
Test run completed. Ran 7 test(s). 7 Passed, 0 Failed
InsertAccessManagementResource_InvalidBearerToken        Passed
InsertAccessManagementResource_ResourcePartialStored     Passed
InsertAccessManagementResource_MissingBearerToken        Passed
InsertAccessManagementResource_ResourceStored            Passed
InsertAccessManagementResource_NoInput                   Passed
InsertAccessManagementResource_InvalidModel              Passed
InsertAccessManagementResource_AllFailed                 Passed
```

Build: clean (0 errors; pre-existing StyleCop warnings only).

## Files Changed

| File | Action |
|---|---|
| `Controllers/ResourceControllerTest.cs` | **Migrated to `ApiFixture`** — removed `CustomWebApplicationFactory<ResourceController>`, `GetTestClient()`, `_factory` field. Added `WithAppsettings` + `ConfigureServices` calls in ctor. |

## Deferred (sub-steps)

Ordered by risk/complexity:

### Sub-step 16.1 — Group A easy wins (2 classes, ✅ completed)

- `PolicyInformationPointControllerTest`
- `DelegationsControllerTest` (registers mocks inline via
  `fixture.ConfigureServices`; the unused `SetupUtils.GetTestClient` overload
  was deleted instead of being rewritten as an `AccessMgmtMocks` extension —
  deferred until a second consumer actually needs it.)

`MaskinportenSchemaControllerTest` was originally planned for 16.1 but
**promoted to 16.2** after inspection: it has ~15 per-test
`_client = GetTestClient(...)` calls that swap `IPDP`, `IHttpContextAccessor`,
or `IDelegationMetadataRepository`. `ApiFixture` seals its DI container on
first `CreateClient()`, so this pattern requires nested-class splitting (rule
6 of the recipe above). See
[AccessMgmt_WAF_Group_A_Easy_Wins.md](AccessMgmt_WAF_Group_A_Easy_Wins.md).

### Sub-step 16.2 — Group A complex (4 classes)

- `MaskinportenSchemaControllerTest` — split into nested classes for the
  `PepWithPDPAuthorizationMock` vs `PdpPermitMock` variants and the per-test
  `HttpContextAccessor` route-value customizations.
- `Altinn2RightsControllerTest`
- `AppsInstanceDelegationControllerTest`
- `RightsInternalControllerTest` — split into two nested classes
  (`WithDefaultMocks` / `WithPdpMock`).

After 16.1 + 16.2: **delete `CustomWebApplicationFactory.cs`** and the
`SetupUtils.GetTestClient` overload. This also unblocks final
`Altinn2RightsClientMock` consolidation (Step 15 deferred item) if the
JSON-backed mock is retired in favour of real DB seeding.

### Sub-step 16.3 — Group B simple (2 classes)

- `HealthCheckTests` → `ApiFixture`.
- `PartyControllerTests` → `ApiFixture` (drop the static lock — `ApiFixture`
  already shares one host per class fixture).

### Sub-step 16.4 — Group B scenario-based (6 classes)

- `ConsentControllerTestBFF`, `ConsentControllerTestEnterprise`,
  `ConsentControllerTest` (MaskinPorten), `V2ResourceControllerTest`,
  `V2MaskinportenSchemaControllerTest`, `V2RightsInternalControllerTest`.
- Port `DelegationScenarios` composition to `ApiFixture.EnsureSeedOnce<TKey>`
  + per-test `HttpClient` built via `fixture.CreateClient()` with
  test-specific headers.
- Re-enable currently `[Skip]`ped V2 tests (they were skipped specifically
  because `WebApplicationFixture` couldn't seed EF test data — `ApiFixture`
  can via `EnsureSeedOnce`).

### Sub-step 16.5 — Retire legacy infrastructure

- Delete `CustomWebApplicationFactory.cs`, `Fixtures/WebApplicationFixture.cs`,
  `Fixtures/PostgresFixture.cs` (incl. `PostgresServer`).
- Rewrite / delete `AcceptanceCriteriaComposer.cs`, `Scenarios/*.cs`,
  `Templates/ControllerTestTemplate.cs`.
- Remove `Yuniql.*` package refs from the project (per Phase 2.6).
- Update `Utils/SetupUtils.cs` — remove the `GetTestClient` overload that
  took `CustomWebApplicationFactory<DelegationsController>`.

## Verification

- ✅ Build: `dotnet build src\apps\Altinn.AccessManagement\test\AccessMgmt.Tests\AccessMgmt.Tests.csproj` — 0 errors.
- ✅ POC test run: `ResourceControllerTest` — 7/7 passed (Podman).
- ⏳ Full regression run of AccessMgmt.Tests — deferred to end of Sub-step 16.5.

## Recommendation for next chat

- **Start a fresh chat for Sub-step 16.1.** Context budget for this step was
  largely consumed by enumeration/audit; the migration itself is now
  recipe-driven and should be fast to execute in batches of 2–3 classes
  per chat.
- Each follow-up chat should open by reading this plan, then the target
  class(es), then apply the recipe.
