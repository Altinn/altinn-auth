# Sub-step 16.2b — AccessMgmt.Tests WAF Consolidation: Group A Nested-Class Splits

Part of Step 16 (Phase 2.2–2.3). See
[AccessMgmt_WAF_Consolidation_Plan_and_POC.md](AccessMgmt_WAF_Consolidation_Plan_and_POC.md)
for the full plan, audit, and migration recipe, and
[AccessMgmt_WAF_Group_A_Single_Config.md](AccessMgmt_WAF_Group_A_Single_Config.md)
for the companion sub-step.

## Goal

Migrate the two remaining Group A consumers that require **sibling-class
splits** (recipe rule 6 — one mutually-exclusive DI configuration per
class) onto the canonical `ApiFixture` pattern, then delete the legacy
`CustomWebApplicationFactory<T>` base class.

## Files affected

- `src/apps/Altinn.AccessManagement/test/AccessMgmt.Tests/Controllers/MaskinportenSchemaControllerTest.cs`
  — migrated main class + new sibling `MaskinportenSchemaPdpPermitControllerTest`.
- `src/apps/Altinn.AccessManagement/test/AccessMgmt.Tests/Controllers/RightsInternalControllerTest.cs`
  — migrated main class + new sibling `RightsInternalControllerWithPdpMockTest`.
- `src/apps/Altinn.AccessManagement/test/AccessMgmt.Tests/CustomWebApplicationFactory.cs`
  — deleted (no remaining consumers).

## What changed

### `MaskinportenSchemaControllerTest.cs`

- `IClassFixture<CustomWebApplicationFactory<MaskinportenSchemaController>>` →
  `IClassFixture<ApiFixture>`.
- Main class default IPDP is `PepWithPDPAuthorizationMock` (majority
  variant). Sibling `MaskinportenSchemaPdpPermitControllerTest` hosts the
  five `PostMaskinportenSchemaDelegation_ValidationProblemDetails_*` tests
  that require `PdpPermitMock`.
- New file-scope `internal sealed class MutableHttpContextAccessor` replaces
  the legacy per-test `HttpContextAccessor` injection. The shared accessor
  supports per-test route-value overrides without rebuilding DI by returning
  a lightweight fake `HttpContext` (copying the real request's `User` and
  headers) whenever an override has been set. Overrides are kept in a plain
  `Dictionary<string, object>` and cleared at the start of each test-class
  constructor — the accessor itself is declared `static` because xUnit
  creates a fresh test-class instance per test while the DI host (built on
  first `CreateClient()`) only ever captures the **first** instance's
  accessor.
- Backward-compatibility shim `GetTestClient(IPDP, IHttpContextAccessor,
  IDelegationMetadataRepository)` kept to minimise the diff inside test
  bodies: reads the caller's mock accessor route values and applies them via
  `SetOverride`; aliases the singleton `DelegationMetadataRepositoryMock`
  with the caller's local variable when provided.

### `RightsInternalControllerTest.cs`

- Same pattern. Main class default IPDP is `PepWithPDPAuthorizationMock`;
  sibling `RightsInternalControllerWithPdpMockTest` registers
  `PdpPermitMock` for its revocation-tests subset.

### `CustomWebApplicationFactory.cs`

- Deleted. No remaining consumers after this sub-step.

## Verification

```text
dotnet test (4 classes via Test Explorer, Podman-backed)
Test run finished: 124 Tests (124 Passed, 0 Failed, 0 Skipped)
```

All four classes are green:

- `MaskinportenSchemaControllerTest` — 61/61
- `MaskinportenSchemaPdpPermitControllerTest` — 4/4
- `RightsInternalControllerTest` — most of the 59
- `RightsInternalControllerWithPdpMockTest` — remainder of 59

Build succeeds after `CustomWebApplicationFactory.cs` deletion.

## Debugging notes

The migration initially produced 9 `InternalServerError` failures in the
`MaskinportenSchemaControllerTest` main class. Two incorrect hypotheses were
explored (merging overrides into the real `HttpContext.Request.RouteValues`
and returning a fake `DefaultHttpContext` whose routing feature was empty)
before the real root-cause surfaced by capturing the server-side response
body inside one failing test: xUnit's `IClassFixture<T>` constructs a new
test-class instance per test, so each `new MutableHttpContextAccessor()` in
the constructor produced a **different** accessor than the one already
registered in DI on first `CreateClient()`. `SetOverride` calls were going
to an orphan instance. The fix is to make the accessor `static` (scoped to
the test class) and clear its overrides at the start of each constructor.

## Follow-ups

None. Sub-step 16.3–16.5 (Group B scenario-based `WebApplicationFixture`
consumers + legacy infrastructure retirement) is next — see INDEX.md.
