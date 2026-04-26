# Step 47 — Fix Phase 6.7e Latent Production Bugs (ServiceOwner API)

## Goal

Fix the three latent production bugs documented in
[Coverage_ServiceOwner_Api.md](Coverage_ServiceOwner_Api.md) and deferred as
Phase 6.7e in the Recommended Next Steps:

1. `PackageConstants.TryGetByName` (via `ConstantLookup.GetByName`) throws
   `ArgumentException` when the package-name dictionary is built because several
   Norwegian package names are not unique (e.g. "Attester", "Byggesøknad").
2. `PackageConstants.TryGetByUrn` Case 2 — the "colon-prefix suffix" path
   (`urn.Split(':').Length == 1`) was unreachable because a string like
   `:jordbruk` splits to `["", "jordbruk"]` (length 2, not 1).
3. `RequestController.CreateResourceRequest` and `.CreatePackageRequest`
   query-param overloads (`POST /resource?from=…` / `POST /package?from=…`)
   returned 400 instead of 202 because the endpoints only bound parameters from
   `[FromBody]`, so a call with no body and query-string params failed model
   binding before the controller logic ever ran.

## What changed

### `ConstantLookup.cs`

`GetByName<TType>` previously used `ToDictionary` which throws
`ArgumentException` on duplicate keys.  Replaced with a `foreach + TryAdd` loop
so the first definition silently wins for any duplicate name and the lookup never
throws:

```csharp
var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
foreach (var cd in constants)
    dict.TryAdd(cd.Entity.Name, cd);
return dict;
```

### `PackageConstants.cs`

Fixed the off-by-one in `TryGetByUrn` Case 2:

```csharp
// Before (unreachable — ":jordbruk".Split(':').Length == 2, not 1):
if (urn.StartsWith(':') && urn.Split(':').Length == 1)

// After:
if (urn.StartsWith(':') && urn.Split(':').Length == 2)
```

### `RequestController.cs` (ServiceOwner)

Both `[HttpPost("resource")]` and `[HttpPost("package")]` action methods were
changed to accept parameters from **either** the request body or the query
string:

```csharp
public async Task<IActionResult> CreateResourceRequest(
    [FromQuery] string? from = null,
    [FromQuery] string? to = null,
    [FromQuery] string? resource = null,
    [FromBody] RequestResourceDto? body = null,
    CancellationToken ct = default)
{
    var data = new RequestResourceDto
    {
        From = body?.From ?? from,
        To   = body?.To   ?? to,
        Resource  = body?.Resource  ?? resource,
        RightKeys = body?.RightKeys,
    };
    // … existing validation + service call unchanged …
}
```

Same pattern applied to `CreatePackageRequest` (`from`/`to`/`package` query
params, nullable body).  Body-only callers are unaffected (body wins over query
when both are present).

### `RequestControllerTest.cs` (ServiceOwner)

| Test | Before | After |
|---|---|---|
| `CreateResourceRequest_WithValidQueryParams_Returns202Accepted` | `[Skip]` — returned 400 (model-binding fail on `[]` body) | Active — returns **202** ✅ |
| `CreateResourceRequest_WithInvalidFromUrn_Returns400` | `PostAsJsonAsync(url, Array.Empty<string>())` — passed for wrong reason (body `[]` caused 400 before auth) | `PostAsync(url, null)` — now reaches controller logic → returns **400** from URN validation ✅ |
| `CreatePackageRequest_WithKnownPackage_ReturnsBadRequest` | Expected 400 (pinned the bug) | Renamed to `Returns202Accepted`, assertion changed to 202 ✅ |
| `CreatePackageRequest_WithInvalidFromUrn_Returns400` | `PostAsJsonAsync(url, Array.Empty<string>())` | `PostAsync(url, null)` ✅ |

### `PackageConstantsTest.cs`

Removed all seven `[Skip]` attributes.  All 7 tests now run and pass,
including `TryGetByAll_WithExistingUrnSuffixWithColon_ReturnsTrue` which
exercises the fixed Case-2 path.

## Verification

```
Run: Project=Altinn.AccessManagement.ServiceOwner.Api.Tests
     Project=Altinn.AccessMgmt.PersistenceEF.Tests
```

| Project | Tests | Result |
|---|---|---|
| ServiceOwner.Api.Tests | 17 | **17 passed, 0 failed** |
| PersistenceEF.Tests (`PackageConstantsTest` — 7) | 7 | **7 passed, 0 failed** |

No regressions in either project.

### Coverage impact

The two previously-skipped / 400-pinned tests (`WithValidQueryParams` and
`WithKnownPackage`) now exercise the full success path of both controller
actions, closing the last ~28% gap in `AccessManagement.Api.ServiceOwner` that
was noted in Step 29.

## Deferred

- `PackageConstants.TryGetByName` name lookup is not called by `TryGetByAll`;
  `TryGetByAll` uses URN → code → GUID order.  If a name-based lookup via
  `TryGetByAll` is ever needed, the duplicate-name policy (first wins) should be
  revisited and a deduplication pass on the constants themselves may be
  warranted.
- The `CreateResourceRequest` and `CreatePackageRequest` success paths still do
  not have validation-failure unit tests for the case where query params are
  supplied but the resource/package does not exist in the registry (only covered
  by integration tests).

---

See [INDEX.md](INDEX.md) for step log and priorities.
