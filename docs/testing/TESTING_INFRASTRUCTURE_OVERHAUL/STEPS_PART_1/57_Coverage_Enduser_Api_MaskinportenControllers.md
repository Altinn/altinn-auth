# Step 57 — Coverage: `AccessManagement.Api.Enduser` — `MaskinportenConsumersController` + `MaskinportenSuppliersController`

## Goal

Close the last uncovered controller gap in `Altinn.AccessManagement.Api.Enduser`
identified at the end of Step 33: `MaskinportenConsumersController` and
`MaskinportenSuppliersController`.

Step 33 had flagged these as requiring "PDP stubbing or seeding of
`altinn_maskinporten_scope_delegation`". That concern applied only to the
WAF/integration path. Because both controllers inject a single service
(`IMaskinportenSupplierService`) with no other constructor dependencies, the
same **direct Moq-based unit-test pattern** used in Steps 49, 52, and 56 is
fully applicable — authorization attributes are never evaluated when the
controller method is called directly.

## What changed

### New test file

`test/Altinn.AccessManagement.Enduser.Api.Tests/Controllers/MaskinportenControllersTest.cs`
— Two test classes, **36 `[Fact]`s** total.

#### `MaskinportenConsumersControllerTest` — 15 tests

| Action | Cases |
|---|---|
| `GetConsumers` | no-filter success → Ok; no-filter service problem → non-Ok; consumer filter → `GetEntity` problem → non-Ok; consumer filter → `GetEntity` ok → Ok |
| `RemoveConsumer` | `GetEntity` problem → non-Ok; success (null problem) → NoContent; `RemoveSupplier` problem → non-Ok |
| `GetResources` | no filters success → Ok; no filters problem → non-Ok; consumer filter `GetEntity` problem; resource filter `GetResourceByRefId` problem; all filters success → Ok |
| `RemoveResource` | `GetEntity` problem; success → NoContent; `RemoveResource` problem |

#### `MaskinportenSuppliersControllerTest` — 21 tests

| Action | Cases |
|---|---|
| `AddSupplier` | `GetEntity` problem; success → Ok; `AddSupplier` problem |
| `GetSuppliers` | no filter success → Ok; no filter problem; supplier filter `GetEntity` problem |
| `RemoveSupplier` | `GetEntity` problem; success → NoContent; `RemoveSupplier` problem |
| `DelegationCheck` | success → Ok (partyUuid claim in HttpContext); problem |
| `AddResource` | `GetEntity` problem; success → Ok; `AddResource` problem |
| `GetResources` | no filters success → Ok; no filters problem; supplier filter `GetEntity` problem; resource filter `GetResourceByRefId` problem |
| `RemoveResource` | `GetEntity` problem; success → NoContent; `RemoveResource` problem |

### Key implementation notes

- `Resource.Id` setter (`BaseResource`) validates UUID v7 — `Guid.NewGuid()` (v4)
  throws at static class initializer. Fix: let the `Resource` default constructor
  assign the v7 UUID (`new Resource()`), then capture `SampleResource.Id`.
- `DelegationCheck` uses `AuthenticationHelper.GetAuthenticatedPartyUuid(HttpContext)`
  which reads `urn:altinn:party:uuid` claim. Tests use `CreateSutWithClaim` helper
  that sets `context.User` with a `ClaimsPrincipal` bearing the claim.
- `RemoveSupplier` / `RemoveResource` in the interface return
  `Task<ValidationProblemInstance>` (not `Task<Result<T>>`). Mocked as
  `ReturnsAsync((ValidationProblemInstance)null)` for the success path.
- `GetLanguageCode()` extension falls back to the default language code when no
  `Accept-Language` header or middleware key is present — no special HttpContext
  setup required.

## Verification

```
MaskinportenConsumersControllerTest → 15 passed, 0 failed, 0 skipped
MaskinportenSuppliersControllerTest → 21 passed, 0 failed, 0 skipped
Total: 36 / 36 passed
```

## Coverage impact

These 36 tests cover all 4 actions of `MaskinportenConsumersController` and all
7 actions of `MaskinportenSuppliersController`, exercising both success and
failure branches for every action.

Estimated line coverage impact on `Altinn.AccessManagement.Api.Enduser`:
**+3–5 pp** (these controllers were 0% covered; they represent the last
significant untested code path in the Enduser API aside from `Program.cs`).

## Remaining gap (after this step)

- `AccessManagementEnduserHost` / `Program` startup — conventionally excluded
  from API coverage expectations.
- `AccessManagement.Persistence` (44.94%) and `AccessMgmt.Persistence` (32.51%)
  — dominated by Npgsql repository code requiring a live database.
