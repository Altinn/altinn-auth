# Sub-step 16.1 — AccessMgmt.Tests WAF Consolidation: Group A Easy Wins

Part of Step 16 (Phase 2.2–2.3). See
[AccessMgmt_WAF_Consolidation_Plan_and_POC.md](AccessMgmt_WAF_Consolidation_Plan_and_POC.md)
for the full plan, audit, and migration recipe.

## Goal

Migrate the easy Group A consumers of
`CustomWebApplicationFactory<TController>` onto the canonical `ApiFixture`
pattern, using the recipe validated in the Step 16 POC
(`ResourceControllerTest`). "Easy" = no per-test DI swaps; all mocks can be
registered once in the class constructor.

## Scope change vs. plan

The plan listed three classes for 16.1:

1. `PolicyInformationPointControllerTest`
2. `DelegationsControllerTest`
3. `MaskinportenSchemaControllerTest`

On inspection, **`MaskinportenSchemaControllerTest` is not an easy win** — it
has ~15 per-test call sites that reassign `_client = GetTestClient(...)` with
different `IPDP` / `IHttpContextAccessor` / `IDelegationMetadataRepository`
implementations. `ApiFixture` seals its DI container on first `CreateClient()`,
so this pattern requires splitting the class into nested classes per the
recipe's rule 6 (same treatment as `RightsInternalControllerTest`). It has
therefore been **moved to Sub-step 16.2**.

Sub-step 16.1 completed **2 of 3** classes; the third was reclassified.

## What changed

### `Controllers/PolicyInformationPointControllerTest.cs` — migrated

- `IClassFixture<CustomWebApplicationFactory<PolicyInformationPointController>>` →
  `IClassFixture<ApiFixture>`.
- Dropped `_factory` field and the private `GetTestClient(...)` helper.
- Constructor now calls `fixture.WithAppsettings(... appsettings.test.json ...)`
  and `fixture.ConfigureServices(...)` registering the same four mocks the
  helper used to register:
  `IDelegationMetadataRepository`, `IPartiesClient`, `IProfileClient`,
  `IResourceRegistryClient`.
- No `IPublicSigningKeyProvider` override — these tests post content directly
  and don't sign tokens via `PrincipalUtil.GetAccessToken`, so the default
  `PublicSigningKeyProviderMock` registered by `ApiFixture` is fine.

### `Controllers/DelegationsControllerTest.cs` — migrated

- `IClassFixture<CustomWebApplicationFactory<DelegationsController>>` →
  `IClassFixture<ApiFixture>`.
- Dropped `_factory` field and the private `GetTestClient(...)` helper.
- Constructor registers all 11 mocks previously wired by `GetTestClient`:
  `IPolicyRetrievalPoint`, `IDelegationMetadataRepository`, `IPolicyFactory`,
  `IDelegationChangeEventQueue`, `IPostConfigureOptions<JwtCookieOptions>`,
  `IPartiesClient`, `IResourceRegistryClient`, `IPDP`
  (`PepWithPDPAuthorizationMock`), `IHttpContextAccessor`, `IAMPartyService`,
  `IAltinnRolesClient`.
- Removes the default `IPublicSigningKeyProvider` registered by `ApiFixture`
  and replaces it with `SigningKeyResolverMock` so tokens signed by
  `PrincipalUtil.GetAccessToken("sbl.authorization")` validate.
- Bearer token is still attached to `_client.DefaultRequestHeaders` in the
  constructor.

### `Utils/SetupUtils.cs` — dead code removed

- Deleted the `GetTestClient(CustomWebApplicationFactory<DelegationsController>)`
  overload and its now-unused usings. It had no remaining callers (the
  consumer had inlined the logic into its own local helper, which was
  replaced above).
- Kept `DeleteAppBlobData` and `AddAuthCookie` — still in use.

## Migration recipe compliance

Both migrations follow the recipe in
[AccessMgmt_WAF_Consolidation_Plan_and_POC.md](AccessMgmt_WAF_Consolidation_Plan_and_POC.md):

- ✅ `WithAppsettings` / `ConfigureServices` called from constructor only.
- ✅ Each class uses its own `IClassFixture<ApiFixture>`.
- ✅ `DelegationsControllerTest` removes the default signing key provider
  before registering `SigningKeyResolverMock`.
- ✅ `appsettings.test.json` preserved.
- ✅ `PolicyInformationPointControllerTest` has no `[Collection(...)]`;
  `DelegationsControllerTest` keeps its existing `[Collection("DelegationController Tests")]`.

## Verification

- ✅ Build: `dotnet build src\apps\Altinn.AccessManagement\test\AccessMgmt.Tests\AccessMgmt.Tests.csproj`
  — 0 errors (pre-existing StyleCop warnings only).
- ✅ Test run (VS Test Explorer, Podman backing `EFPostgresFactory`):
  `PolicyInformationPointControllerTest` + `DelegationsControllerTest`
  → **51 / 51 passed**, 0 failed.

## Deferred

- `MaskinportenSchemaControllerTest` — moved to Sub-step 16.2. Needs
  nested-class splitting for the `PepWithPDPAuthorizationMock` /
  `PdpPermitMock` variants and per-test `HttpContextAccessor` route-value
  customization. Estimated effort is comparable to
  `RightsInternalControllerTest`.
- Deleting `CustomWebApplicationFactory.cs` is still blocked on the
  remaining Group A consumers (`MaskinportenSchemaControllerTest`,
  `Altinn2RightsControllerTest`, `AppsInstanceDelegationControllerTest`,
  `RightsInternalControllerTest`). Tracked under 16.2.

## Recommendation for next chat

Start a fresh chat for Sub-step 16.2. Context budget for this step was modest,
but 16.2 is materially more complex (nested-class splitting across three
classes, plus `MaskinportenSchemaControllerTest` now promoted into it) and
will benefit from a clean context window.
