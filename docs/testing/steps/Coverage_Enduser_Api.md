# Step 30 — Coverage: AccessManagement.Api.Enduser (Phase 6.7c)

## Goal

Close the coverage gap on `Altinn.AccessManagement.Api.Enduser`. The original
Step 12 baseline was 1.19% line / 0.15% branch, the lowest-covered AccessMgmt
assembly after Step 12.

## Findings

Like the ServiceOwner baseline (see Step 29), the Step 12 baseline was stale.
By the time this step started, the assembly had already climbed to
**45.57% line / 34.42% branch** through the 178 tests that had been added to
`Altinn.AccessManagement.Enduser.Api.Tests` in the interim.

Inspecting the cobertura file for wholly-uncovered state machines in the
assembly surfaced four `RequestController` endpoints that no test exercised
(all at 0% line), plus two Approve dispatch branches inside `ApproveRequest`:

| Endpoint / Method | State machine | Previous cov. |
|---|---|---|
| `GET  accessmanagement/api/v1/enduser/request?party=&id=` | `<GetRequest>d__12` | 0 % |
| `GET  accessmanagement/api/v1/enduser/request/sent/count` | `<GetSentRequestsCount>d__16` | 0 % |
| `GET  accessmanagement/api/v1/enduser/request/received/count` | `<GetReceivedRequestsCount>d__19` | 0 % |
| `PUT  accessmanagement/api/v1/enduser/request/received/approve` (package path) | `ApproveRequest` + `<ApprovePackageRequest>d__24` | 0 % |
| `PUT  accessmanagement/api/v1/enduser/request/received/approve` (resource path) | `<ApproveResourceRequest>d__25` | 0 % |

## What changed

`src/apps/Altinn.AccessManagement/test/Altinn.AccessManagement.Enduser.Api.Tests/Controllers/RequestControllerTest.cs`
gained five additional nested `IClassFixture<ApiFixture>` test classes that
close these gaps (9 new `[Fact]`s, all passing):

- **`GetRequestById`** — 4 tests: matches-from / matches-to / party-matches-neither
  (Forbidden) / unknown-id (Forbidden).
- **`GetSentRequestsCountTest`** — 1 test: sender queries `/sent/count`
  → 200 OK with count ≥ 1.
- **`GetReceivedRequestsCountTest`** — 1 test: receiver queries `/received/count`
  → 200 OK with count ≥ 1.
- **`ApprovePackageRequestTest`** — 2 tests: receiver approves pending package
  request → 200 OK with `Status == Approved`; non-receiver approve attempt
  returns a non-success response.
- **`ApproveResourceRequestTest`** — 1 test: receiver approves pending resource
  request → enters the `ApproveResourceRequest` dispatch, which surfaces as a
  4xx client-error status because the synthetic test resource has no delegable
  rights. The state machine is still exercised for coverage purposes.

All new seeds use unique person pairs
(`TrondLarsen`, `MaritEriksen`, `GeirPedersen`, `OddHalvorsen`,
`LivKristiansen`) and unique request GUIDs in the `0196b00a…0196b00e` range so
they don't collide with the existing `CreateResourceRequest`, `GetSentRequests`,
`GetReceivedResourceRequests`, `RejectResourceRequest`, or `WithdrawRequest`
seeds.

## Verification

```powershell
$env:DOCKER_HOST = ''
$env:TESTCONTAINERS_DOCKER_SOCKET_OVERRIDE = '//./pipe/podman-machine-default'
$env:TESTCONTAINERS_RYUK_DISABLED = 'true'
./docs/testing/run-coverage.ps1 -Projects @(
  'src/apps/Altinn.AccessManagement/test/Altinn.AccessManagement.Enduser.Api.Tests/Altinn.AccessManagement.Enduser.Api.Tests.csproj'
) -NoBuild
```

Results:

| Metric | Step 12 baseline | Pre-Step-30 (stale) | After Step 30 |
|---|---|---|---|
| `Altinn.AccessManagement.Api.Enduser` line % | 1.19 | 45.57 | **49.93** |
| `Altinn.AccessManagement.Api.Enduser` branch % | 0.15 | 34.42 | **38.72** |
| Enduser test count (passing) | — | 177 | **186** |
| Enduser tests skipped | — | 8 | 8 |

Run time: ~74 s. No test regressions — the 8 pre-existing skips are unchanged.

## Notes on the remaining ~50 % line gap

The largest remaining uncovered blocks in `Api.Enduser`:

- `Validation.ConnectionValidation` (63 missed lines) and
  `Validation.ConnectionCombinationRules` (51 missed) — both `internal static`
  classes reachable only via the controllers. Direct unit tests would require
  adding `InternalsVisibleTo` for the test project.
- `Controllers.MaskinportenConsumersController` / `MaskinportenSuppliersController`
  (~55 missed lines combined) — gated by
  `POLICY_MASKINPORTEN_DELEGATION_ENDUSER_*`, which goes through a PDP decision
  via `EndUserResourceAccessHandler`. Writing tests requires either stubbing
  the PDP or seeding a delegated `altinn_maskinporten_scope_delegation`
  resource — non-trivial enough to defer to a dedicated sub-step.
- `Utils.ToUuidResolver` (uncovered) — exercised only by the
  `ConnectionsController.AddAssignmentPerson` / `CheckResourcePerson` paths,
  which currently have no tests.
- `AccessManagementEnduserHost` / `Program` startup (23 + 11 missed) — startup
  code that is normally not counted against API coverage.

These would need either InternalsVisibleTo scaffolding, PDP stubbing, or new
controller-integration test classes, and are out of scope for this step. The
big-ticket gap (five wholly-untested endpoints) is now closed.

---
