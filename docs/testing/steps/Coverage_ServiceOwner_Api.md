# Step 29 — Coverage: AccessManagement.Api.ServiceOwner (Phase 6.7b)

## Goal

Close the coverage gap on `Altinn.AccessManagement.Api.ServiceOwner` (baseline
0.00% line / 0.00% branch in Step 12).

## Findings

The Step 12 baseline was stale: by the time this step started, the assembly
already sat at **54.35% line / 50.00% branch**, because
`Altinn.AccessManagement.ServiceOwner.Api.Tests` had been expanded in the
interim. All test coverage flows through `ApiFixture` (referenced via
`TestUtils`) — the test project project-references the `Api.ServiceOwner`
project so the ServiceOwner controllers are auto-registered as ApplicationParts
when the main `Altinn.AccessManagement` host boots.

A coverage-file inspection identified three public `RequestController`
endpoints that were entirely uncovered:

| Endpoint | State machine | Previous cov. |
|---|---|---|
| `GET  accessmanagement/api/v1/serviceowner/delegationrequests/{id}/status` | `<GetRequestStatus>d__7` | 0 % |
| `POST accessmanagement/api/v1/serviceowner/delegationrequests/resource` (query-param overload) | `<CreateResourceRequest>d__8` | 0 % |
| `POST accessmanagement/api/v1/serviceowner/delegationrequests/package`  (query-param overload) | `<CreatePackageRequest>d__9`  | 0 % |

## What changed

`src/apps/Altinn.AccessManagement/test/Altinn.AccessManagement.ServiceOwner.Api.Tests/Controllers/RequestControllerTest.cs`
now has three additional nested `IClassFixture<ApiFixture>` test classes that
close these gaps (7 new `[Fact]`s):

- `GetRequestStatusTest`
  - `GetRequestStatus_ForExistingRequest_ReturnsOk` — creates a request via
    `POST /` and then reads `/{id}/status` using a combined read+write scope
    token (new `CreateReadWriteClient` helper).
  - `GetRequestStatus_ForUnknownId_ReturnsBadRequest` — unknown GUID → 400.
  - `GetRequestStatus_Unauthenticated_ReturnsUnauthorized` — anonymous → 401.
- `CreateResourceRequestByQueryTest`
  - `CreateResourceRequest_WithValidQueryParams_Returns202Accepted` — happy
    path for the query-param overload.
  - `CreateResourceRequest_WithInvalidFromUrn_Returns400`.
- `CreatePackageRequestByQueryTest`
  - `CreatePackageRequest_WithKnownPackage_ReturnsBadRequest` — exercises the
    endpoint with a valid package URN. **Pins a latent controller bug**: the
    query-param overload forwards the resolved `packageObj.Id` as
    `RequestReferenceDto.Id` (but leaves `ReferenceId` null) to the private
    `CreatePackageRequest` helper, which re-runs `PackageConstants.TryGetByAll`
    on the null `ReferenceId` and always adds `PackageNotExists`. Fixing that
    bug is out of scope for a coverage step. The test carries a comment so the
    next person who touches the controller can flip the expectation.
  - `CreatePackageRequest_WithInvalidFromUrn_Returns400` — short-circuits via
    URN validation before the package-lookup codepath.

Note: an "unknown package URN" test case cannot currently be written because
`PackageConstants.TryGetByName` throws for any string that isn't a registered
URN (duplicate-key `ArgumentException` when the name dictionary is built),
surfacing as HTTP 500. That is another pre-existing production issue in
`PackageConstants` and is also out of scope here.

New helper: `CreateReadWriteClient(ApiFixture, orgNo)` — emits a token whose
`scope` claim is a space-delimited combination of
`altinn:accessmanagement/serviceowner/delegationrequests.write` and
`…read`, which `ScopeAccessHandler` already splits on spaces.

## Verification

```powershell
$env:DOCKER_HOST = ''
$env:TESTCONTAINERS_DOCKER_SOCKET_OVERRIDE = '//./pipe/podman-machine-default'
$env:TESTCONTAINERS_RYUK_DISABLED = 'true'
./docs/testing/run-coverage.ps1 -Projects @(
  'src/apps/Altinn.AccessManagement/test/Altinn.AccessManagement.ServiceOwner.Api.Tests/Altinn.AccessManagement.ServiceOwner.Api.Tests.csproj'
) -NoBuild
```

Results:

| Metric | Before | After |
|---|---|---|
| `Altinn.AccessManagement.Api.ServiceOwner` line % | 54.35 | **71.74** |
| `Altinn.AccessManagement.Api.ServiceOwner` branch % | 50.00 | **60.00** |
| ServiceOwner test count | 11 | **18** (all passing) |

Run time: ~17 s. No test regressions.

## Notes on the remaining ~28 % line gap

The uncovered remainder consists of:

- The query-param package overload's success tail (blocked by the
  `ReferenceId`/`Id` bug above).
- Some validation branches inside `CreateRequest` that require unusual input
  combinations already exercised indirectly by `CreateRequestTest`.
- Minor `BuildLinks` branches that only differ by `GeneralSettings.Hostname`.

These would need either a controller fix or more elaborate test scaffolding;
the big-ticket gap (three wholly-untested endpoints) is now closed.

---

See [INDEX.md](INDEX.md) for step log and priorities.
