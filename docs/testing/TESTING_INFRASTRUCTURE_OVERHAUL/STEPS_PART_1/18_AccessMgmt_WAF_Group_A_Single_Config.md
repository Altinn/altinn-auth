# Sub-step 16.2a — AccessMgmt.Tests WAF Consolidation: Group A Single-Configuration Migrations

Part of Step 16 (Phase 2.2–2.3). See
[AccessMgmt_WAF_Consolidation_Plan_and_POC.md](AccessMgmt_WAF_Consolidation_Plan_and_POC.md)
for the full plan, audit, and migration recipe.

## Goal

Migrate the two remaining Group A consumers that use a **single** DI
configuration for every test onto the canonical `ApiFixture` pattern. This
is a straightforward application of the Step 16 POC recipe — no
nested-class splitting required.

## Scope change vs. plan

The plan listed Sub-step 16.2 as four classes:

1. `MaskinportenSchemaControllerTest` (needs nested-class split)
2. `Altinn2RightsControllerTest`
3. `AppsInstanceDelegationControllerTest`
4. `RightsInternalControllerTest` (needs nested-class split)

On inspection, items 2 and 3 share the same migration shape as 16.1's easy
wins — a single mock set registered once in the constructor, with per-test
`HttpClient` instances created via `fixture.CreateClient()`. Items 1 and 4
genuinely require nested-class splitting (multiple mutually exclusive mock
configurations per class) and a materially larger context budget (~1700
lines each).

Sub-step 16.2 has therefore been split into:

- **16.2a (this doc)** — the two single-configuration migrations.
- **16.2b** — `MaskinportenSchemaControllerTest` and
  `RightsInternalControllerTest`, each split into nested classes per
  recipe rule 6. Tracked in INDEX.md as the new highest-priority next step.

## What changed

### `Controllers/Altinn2RightsControllerTest.cs` — migrated

- `IClassFixture<CustomWebApplicationFactory<RightsInternalController>>` →
  `IClassFixture<ApiFixture>`.
- Dropped `_factory` field; added `_fixture` field.
- Constructor calls
  `fixture.WithAppsettings(... appsettings.test.json ...)` and
  `fixture.ConfigureServices(WithServiceMoq)`.
- Replaced the helper pair `NewServiceCollection(...)` +
  `NewClient(WebApplicationFactory<RightsInternalController>, ...)` with a
  single instance method `NewClient(params Action<HttpClient>[])` that
  delegates to `_fixture.CreateClient()`. `NewDefaultClient` now calls
  it directly.
- The two `ClearAccessCache` tests updated to use the new
  `NewClient(WithClientRoute(...))` signature (previously
  `NewClient(NewServiceCollection(WithServiceMoq), WithClientRoute(...))`).
- `WithServiceMoq` now performs `services.RemoveAll<IPublicSigningKeyProvider>()`
  before registering `SigningKeyResolverMock`, per recipe rule 3
  (ApiFixture registers `PublicSigningKeyProviderMock` by default).

### `Controllers/ResourceOwnerAPI/AppsInstanceDelegationControllerTest.cs` — migrated

- `IClassFixture<CustomWebApplicationFactory<AppsInstanceDelegationController>>`
  → `IClassFixture<ApiFixture>`.
- Dropped `_factory` field; added `_fixture` field.
- Constructor calls `fixture.WithAppsettings` + `fixture.ConfigureServices`
  registering all 10 mocks previously wired by `GetTestClient`:
  `IPolicyRetrievalPoint`, `IDelegationMetadataRepository`, `IPolicyFactory`,
  `IPostConfigureOptions<JwtCookieOptions>`, `IPartiesClient`, `IProfileClient`,
  `IResourceRegistryClient`, `IAltinnRolesClient`, `IPDP` (`PdpPermitMock`),
  `IAltinn2RightsClient`.
- Removes the default `IPublicSigningKeyProvider` before registering
  `SigningKeyResolverMock` (recipe rule 3).
- `GetTestClient(string token, params Action<IServiceCollection>[])`
  simplified to `GetTestClient(string token)` — the variadic overload was
  never exercised (confirmed by callsite search). The dead
  `WithPDPMock` helper in this file was removed at the same time.

## Migration recipe compliance

Both migrations follow the recipe in
[AccessMgmt_WAF_Consolidation_Plan_and_POC.md](AccessMgmt_WAF_Consolidation_Plan_and_POC.md):

- ✅ `WithAppsettings` / `ConfigureServices` called from constructor only.
- ✅ Each class uses its own `IClassFixture<ApiFixture>`.
- ✅ Default `IPublicSigningKeyProvider` removed before registering
  `SigningKeyResolverMock`.
- ✅ `appsettings.test.json` preserved.
- ✅ No `[Collection(...)]` attribute on either class (neither had one to
  begin with).

## Verification

- ✅ Build: `dotnet build src\apps\Altinn.AccessManagement\test\AccessMgmt.Tests\AccessMgmt.Tests.csproj`
  — 0 errors (pre-existing StyleCop warnings only).
- ✅ Test run (VS Test Explorer, Podman backing `EFPostgresFactory`):
  `Altinn2RightsControllerTest` + `AppsInstanceDelegationControllerTest`
  → **23 / 23 passed**, 0 failed, in 28 sec.

## Deferred

- `MaskinportenSchemaControllerTest` and `RightsInternalControllerTest` —
  moved to new Sub-step 16.2b. Both need nested-class splitting for
  mutually exclusive mock configurations (`PepWithPDPAuthorizationMock`
  vs `PdpPermitMock`, plus per-test `HttpContextAccessor` route-value
  customization in `MaskinportenSchemaControllerTest`).
- Deleting `CustomWebApplicationFactory.cs` remains blocked on 16.2b.

## Recommendation for next chat

Start a fresh chat for Sub-step 16.2b. The two remaining classes total
~3600 lines and each requires careful nested-class splitting per recipe
rule 6. A clean context window is warranted.
