# Sub-step 16.3 — AccessMgmt.Tests WAF Consolidation: Group B Simple

Part of Step 16 (Phase 2.2–2.3). See
[AccessMgmt_WAF_Consolidation_Plan_and_POC.md](AccessMgmt_WAF_Consolidation_Plan_and_POC.md)
for the full plan, audit, and migration recipe.

## Goal

Migrate the two simplest Group B consumers — `HealthCheckTests` and
`PartyControllerTests` — from `WebApplicationFixture` onto the canonical
`ApiFixture` pattern. These two classes either use no scenarios at all
(`HealthCheckTests`) or use a hand-rolled static-shared-factory pattern
(`PartyControllerTests`) that `ApiFixture` supersedes natively.

## Files affected

- `src/apps/Altinn.AccessManagement/test/AccessMgmt.Tests/HealthCheckTests.cs`
- `src/apps/Altinn.AccessManagement/test/AccessMgmt.Tests/Controllers/PartyControllerTests.cs`

## What changed

### `HealthCheckTests.cs`

- `IClassFixture<WebApplicationFixture>` → `IClassFixture<ApiFixture>`.
- Removed the `Fixture.ConfigureHostBuilderWithScenarios().Client` lookup
  in each test body; the client is now built once in the constructor via
  `fixture.CreateClient()`.
- Preserved `appsettings.test.json` via `fixture.WithAppsettings(...)` so
  the Azure Storage / Cosmos / feature-flag keys the legacy fixture
  depended on continue to be available (even though the health endpoints
  themselves do not consume them).
- Health endpoints have no DI dependencies of their own, so no
  `ConfigureServices` callback is needed.

### `PartyControllerTests.cs`

- `IClassFixture<WebApplicationFixture>` → `IClassFixture<ApiFixture>`.
- Deleted the manual static-shared-factory pattern
  (`_sharedFactory` + `_factoryLock`): `ApiFixture` already provides a
  per-class-fixture host, so a second layer of caching is redundant.
- Deleted the `[Collection("Internal PartyController Test")]` attribute
  per recipe rule 5 — `IClassFixture<ApiFixture>` already isolates per
  class, and no cross-class parallelism hazard exists.
- Moved the DI overrides (`JwtCookiePostConfigureOptionsStub`,
  `SigningKeyResolverMock`) into `fixture.ConfigureServices(...)`, using
  `RemoveAll<IPublicSigningKeyProvider>()` before re-adding the
  resolver mock (recipe rule 3) so the issuer-cert-based mock wins over
  `ApiFixture`'s default `PublicSigningKeyProviderMock`.
- Kept the private `GetClient()` helper (now a trivial `=> _client`
  accessor) to minimise the diff inside test bodies.

## Verification

```text
dotnet build src\apps\Altinn.AccessManagement\test\AccessMgmt.Tests\AccessMgmt.Tests.csproj
Build succeeded (pre-existing StyleCop + xUnit1051 warnings only; 0 errors).

Test run completed. Ran 8 test(s). 8 Passed, 0 Failed
  HealthCheckTests — 2/2
  PartyControllerTests — 6/6
```

## Follow-ups

Sub-step 16.4 — the six scenario-based Group B consumers
(`ConsentControllerTestBFF`, `ConsentControllerTestEnterprise`,
`ConsentControllerTest` (MaskinPorten), `V2ResourceControllerTest`,
`V2MaskinportenSchemaControllerTest`, `V2RightsInternalControllerTest`)
— is next. That work requires porting `DelegationScenarios` composition
to `ApiFixture.EnsureSeedOnce<TKey>` and will also re-enable the
currently `[Skip]`ped V2 tests.

`WebApplicationFixture` / `PostgresFixture` / `PostgresServer` cannot be
deleted yet — those six classes still depend on them. Retirement is
deferred to Sub-step 16.5.
