# Step 6.7d (Part 5) — Coverage: Api.Internal Extensions + Integration EventMapper

## Goal

Continue Phase **6.7d** by adding pure-logic unit tests for two gap assemblies:

| Assembly | Line% (before) | Target |
|---|---|---|
| `AccessManagement.Api.Internal` | 46.74% | ↑ pure-logic tests for all 5 extension classes + `UserUtil` + `PagingInput` |
| `AccessManagement.Integration` | 47.57% | ↑ `EventMapperService` mapping tests |

---

## What Changed

### 1. `Altinn.AccessManagement.Api.Internal.csproj` — `InternalsVisibleTo`

Added an explicit `<InternalsVisibleTo Include="Altinn.AccessManagement.Api.Tests" />`
item group. The `Directory.Build.targets` auto-generates only `<AssemblyName>.Tests`
(i.e. `Altinn.AccessManagement.Api.Internal.Tests`) — without this addition the
`internal` methods `PagingInput.ToOpaqueToken()` and `PagingInput.CreateFromToken()`
would not be accessible from the test project.

### 2. `Altinn.AccessManagement.Api.Tests.csproj` — new project reference

Added `<ProjectReference>` for `Altinn.AccessManagement.Api.Internal` so the new
tests can reach the extension methods, `UserUtil`, and `PagingInput` directly.

### 3. New test file — `Extensions/ConsentExtensionsTest.cs` (22 tests)

In `Altinn.AccessManagement.Api.Tests` — covers every extension method and utility
in `AccessManagement.Api.Internal`:

| Source | Methods / scenarios tested |
|---|---|
| `ConsentResourceAttributeExtensions` | `ToConsentResourceAttributeExternal` — type + value mapping |
| `ConsentRightExtensions` | `ToConsentRightExternal` — empty resource list; non-empty with two attributes |
| `ConsentContextExternalExtensions` | `ToConsentContext` — language mapping |
| `ConsentRequestEventExtensions` | `ToConsentRequestEventExternal` — all 3 valid URN types (`OrganizationId`, `PersonId`, `PartyUuid`) + unknown type → `ArgumentException` |
| `ConsentRequestDetailsExtensions` | `ToConsentRequestDetailsBFF` — happy path (scalars); optional `RequiredDelegator`/`HandledBy`; `PortalViewMode.Hide`; `PortalViewMode.Show`; out-of-range enum value → `null`; `To` not `PartyUuid` → `ArgumentException`; `From` not `PartyUuid` → `ArgumentException` |
| `UserUtil` | `GetUserUuid` — null principal; no matching claim; valid UUID claim; non-UUID value |
| `PagingInput` | `ToOpaqueToken`/`CreateFromToken` round-trip; default-value round-trip; `GetExamples()` values |

### 4. New test file — `Services/EventMapperServiceTest.cs` (3 tests)

In `AccessMgmt.Tests` — covers `EventMapperService.MapToDelegationChangeEventList`:

| Scenario | Assertion |
|---|---|
| Empty input list | `DelegationChangeEvents` is empty |
| Single `DelegationChange` | All 7 mapped fields equal source values |
| 5 changes | Count = 5; all `DelegationChangeId` values present |

`AccessManagement.Integration` is a transitive dependency of `Altinn.AccessManagement`
(already referenced by `AccessMgmt.Tests`), so no new project reference was required.

---

## Verification

```
Altinn.AccessManagement.Api.Tests  → 24 Passed, 0 Failed, 0 Skipped
  (22 new ConsentExtensionsTest + 2 pre-existing)
AccessMgmt.Tests (EventMapperServiceTest) → 3 Passed, 0 Failed, 0 Skipped
```

Build: **0 errors** on both projects.

---

## Deferred

- `AccessManagement.Integration` HTTP clients (`ResourceRegistryClient`,
  `PartiesClient`, `AltinnRolesClient`, etc.) — these wrap `HttpClient` and
  require mock `HttpMessageHandler`; deferred as medium-effort integration-test
  work.
- `AccessManagement.Api.Internal` controllers (`InternalConnectionsController`,
  `SystemUserClientDelegationController`, `PartyController`, BFF
  `ConsentController`) — require WAF or Moq-based controller-test harness;
  deferred to a later step.
- `AccessManagement.Persistence` and `AccessMgmt.Persistence` gap assemblies —
  majority of code is Npgsql/Postgres repository implementations; pure-logic
  services (e.g. `ConnectionService`, `PackageService`) call repositories and
  would need extensive Moq setup; deferred.
