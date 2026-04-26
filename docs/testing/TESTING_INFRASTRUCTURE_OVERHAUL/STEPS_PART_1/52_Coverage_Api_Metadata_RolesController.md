# Step 52 — Coverage: `AccessManagement.Api.Metadata` `RolesController` (Phase 6.7d continued)

## Goal

Continue Phase **6.7d** by unit-testing `RolesController` in `Altinn.AccessManagement.Api.Metadata`
— the only remaining untested controller in that assembly after Step 42
(`PackagesController` + `TypesController.GetOrganizationSubTypes`).

`RolesController` has six public endpoints, all of which perform constant-lookup validation
(`RoleConstants.TryGetByCode`, `EntityVariantConstants.TryGetByName`) before delegating to
`IRoleService` and running deep-translation via `ITranslationService`.  All branches are
exercisable without any container runtime.

---

## What Changed

### New test file — `Controllers/Metadata/RolesControllerTest.cs` (14 tests)

Located in `src/apps/Altinn.AccessManagement/test/AccessMgmt.Tests/Controllers/Metadata/`.

Tests use direct controller instantiation (no `WebApplicationFactory`) with a `DefaultHttpContext`
carrying an `Accept-Language` header and Moq mocks for `IRoleService` and `ITranslationService`.

The `ITranslationService` mock is configured as a pass-through for every DTO type that
`TranslateDeepAsync` extension methods call (`RoleDto`, `ProviderDto`, `ProviderTypeDto`,
`PackageDto`, `AreaDto`, `AreaGroupDto`, `ResourceDto`, `TypeDto`).

| Endpoint | Method signature | Cases covered |
|---|---|---|
| `GetAll` | `Task<ActionResult<List<RoleDto>>> GetAll()` | Results found → 200 OK; service returns null → 404 |
| `GetId` | `Task<ActionResult<RoleDto>> GetId(Guid id)` | Found → 200 OK; service returns null → 404 |
| `GetPackages` (by code) | `GetPackages(string role, string variant, ...)` | Bad role code → 404; bad variant → 404; both valid → 200 OK |
| `GetResources` (by code) | `GetResources(string role, string variant, ...)` | Bad role code → 404; bad variant → 404; both valid → 200 OK |
| `GetPackages` (by id) | `GetPackages(Guid id, string variant, ...)` | Bad variant → 404; valid → 200 OK |
| `GetResources` (by id) | `GetResources(Guid id, string variant, ...)` | Bad variant → 404; valid → 200 OK |

Known-good constants used: `KnownRoleCode = "rettighetshaver"` (from `RoleConstants.Rightholder`),
`KnownVariantName = "UTBG"` (from `EntityVariantConstants.UTBG`).

---

## Verification

```
Test run completed. Ran 14 test(s). 14 Passed, 0 Failed, 0 Skipped
```

All 14 new tests are pure in-memory — no container, no database, no network.

---

## Deferred

No further pure-logic coverage gaps remain in `AccessManagement.Api.Metadata` that are
achievable without a container.  The only remaining paths in this assembly are the
`Program.cs` / `AccessManagementMetadataHost.cs` startup paths (not unit-testable without
integration infrastructure).
