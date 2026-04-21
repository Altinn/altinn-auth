# Step 33 — Coverage: AccessManagement.Api.Enduser `ToUuidResolver` (Phase 6.7c continued)

## Goal

Continue closing the `Altinn.AccessManagement.Api.Enduser` coverage gap
flagged at the end of Step 32 by direct-unit-testing `Utils.ToUuidResolver`.
This helper is reached at runtime only through
`ConnectionsController.AddAssignmentPerson` / `CheckResourcePerson`, so each
branch would otherwise require an expensive controller-level integration
test. The class is `internal` and already exposed to the test assembly via
the `InternalsVisibleTo` item added in Step 31, so no production-code
changes were required.

## What changed

### New test file

`test/Altinn.AccessManagement.Enduser.Api.Tests/Utils/ToUuidResolverTest.cs`
— **13 `[Fact]`s** using Moq (`MockBehavior.Strict`) for the two injected
services (`IEntityService`, `IUserProfileLookupService`) and
`DefaultHttpContext` for the claims-bearing context.

Coverage per method:

| Method | Cases covered |
|---|---|
| `ResolveWithConnectionInputAsync` | non-person entity (success); entity not found (`PartyNotFound`); person entity with `allowConnectionInputForPersonEntity=false` (`PersonInputRequiredForPersonAssignment`); person entity with `allowConnectionInputForPersonEntity=true` (success) |
| `ResolveWithPersonInputAsync` | SSN path (11 digits, all-digit) → profile resolved; username path (non-11-char identifier); 11-char identifier containing non-digits → username path; `UserUuid.Empty` falls back to `Party.PartyUuid`; lookup returns `null` (validation error); profile has no resolvable uuid (`PartyNotFound`); `Party.PartyUuid == Guid.Empty` (`PartyNotFound`); `TooManyFailedLookupsException` → 429 `ObjectResult`; missing `urn:altinn:userid` claim → `authUserId=0` forwarded |

## Verification

New tests run in isolation (xUnit v3 exe, direct):

```
Altinn.AccessManagement.Enduser.Api.Tests.Utils.ToUuidResolverTest
→ 13 passed, 0 failed, 0 skipped, 2.245s
```

Full Enduser test project with coverage (Release, Podman socket):

```powershell
$env:DOCKER_HOST = ''
$env:TESTCONTAINERS_DOCKER_SOCKET_OVERRIDE = '//./pipe/podman-machine-default'
$env:TESTCONTAINERS_RYUK_DISABLED = 'true'
./docs/testing/run-coverage.ps1 -Projects @(
  'src/apps/Altinn.AccessManagement/test/Altinn.AccessManagement.Enduser.Api.Tests/Altinn.AccessManagement.Enduser.Api.Tests.csproj'
) -NoBuild
```

| Metric | After Step 32 | After Step 33 |
|---|---|---|
| `Altinn.AccessManagement.Api.Enduser` line % | 65.94 | **68.32** |
| `Altinn.AccessManagement.Api.Enduser` branch % | 55.49 | **58.90** |
| Enduser total tests (all classes) | 291 | **304** (+13) |
| Enduser failed | 18 flaky | 0 |

The same full-project run that previously surfaced 18 environment-dependent
integration failures (Step 32) completed cleanly this time (0 failed, 8
skipped). The 13 new unit tests are deterministic and pass in isolation; the
improvement in the flaky suite is unrelated and environmental.

## Remaining gap

With `ToUuidResolver` now covered, the last `Api.Enduser` blocks still
uncovered are:

- `Controllers.MaskinportenConsumersController` /
  `MaskinportenSuppliersController` — gated by
  `POLICY_MASKINPORTEN_DELEGATION_ENDUSER_*` (PDP decision via
  `EndUserResourceAccessHandler`); still needs PDP stubbing or seeding of
  the `altinn_maskinporten_scope_delegation` delegated resource.
- `AccessManagementEnduserHost` / `Program` startup — typically excluded
  from API coverage expectations.

---

**Next:** The remaining Enduser follow-up (Maskinporten controllers)
requires controller-level integration tests with PDP stubbing — a distinct
effort from the unit-test direction taken in Steps 31–33. A fresh chat
focused on **Phase 6.7d** (AccessMgmt persistence layers, 8–45% coverage)
is likely the higher-priority use of the next slot.
