# Step 31 — Coverage: AccessManagement.Api.Enduser Validation layer (Phase 6.7c continued)

## Goal

Close the single largest remaining coverage gap in
`Altinn.AccessManagement.Api.Enduser` identified by Step 30: the two
`internal static` validation classes `ConnectionValidation` and
`ConnectionCombinationRules` (114 uncovered lines combined). These rules are
reachable only through the Enduser controllers, which makes every edge case
expensive to exercise end-to-end.

## What changed

### 1. Exposed the internal validation API to the test assembly

`src/apps/Altinn.AccessManagement/src/Altinn.AccessManagement.Api.Enduser/Altinn.AccessManagement.Api.Enduser.csproj`
gained an explicit `InternalsVisibleTo` item for the test assembly. The repo
convention in `src/Directory.Build.targets` already adds
`<InternalsVisibleTo Include="$(AssemblyName).Tests" />` automatically for
non-test projects, but the Enduser test assembly is named
`Altinn.AccessManagement.Enduser.Api.Tests` (the `Enduser`/`Api` segments are
swapped), so the default does not match.

```xml
<ItemGroup>
    <InternalsVisibleTo Include="Altinn.AccessManagement.Enduser.Api.Tests" />
</ItemGroup>
```

### 2. Added direct unit tests under `Validation/`

Two new test classes, both following the existing FluentAssertions-first
convention (see `docs/testing/FLUENT_ASSERTIONS_GUIDELINES.md`):

- **`Validation/ConnectionCombinationRulesTest.cs`** — 32 `[Fact]`s covering
  the five cross-field rules exposed by `ConnectionCombinationRules`:
  - `PartyEqualsFrom` — invalid party, invalid from, empty-guid party/from,
    matching, mismatching.
  - `PartyMatchesFromOrTo` — invalid/empty party, neither from nor to valid,
    party matches from, party matches to, neither matches (both valid →
    two errors, only-from valid → from error, only-to valid → to error).
  - `RemovePartyMatchesFromOrTo` — any of party/from/to invalid or empty,
    party matches from, party matches to, no match (two errors).
  - `FromAndToMustBeDifferent` — from invalid, to invalid/empty, different,
    same (two errors, self-delegation).
  - `ExclusivePackageReference` — only urn, only id, both provided (two
    errors), id=`Guid.Empty`, neither provided, custom parameter names.

  Error assertions use `Paths[0]` (e.g. `"QUERY/from"`) rather than
  `Detail`, because `Detail` always carries the descriptor text
  (`"One or more query parameters are invalid."`); the per-field message is
  attached as `ProblemExtensionData` extensions on
  `ValidationErrorInstance`.

- **`Validation/ConnectionValidationTest.cs`** — 21 `[Fact]`s exercising
  each of the 11 composing methods on `ConnectionValidation`
  (`ValidateReadConnection`, `ValidateAddAssignmentWithConnectionInput`,
  both `ValidateAddAssignmentWithPersonInput` overloads,
  `ValidateAddPackageToConnectionWithConnectionInput`/`WithPersonInput`,
  `ValidateRemoveConnection`, `ValidateRemovePackageFromConnection`,
  `ValidateAddResourceToConnectionWithConnectionInput`/`WithPersonInput`,
  `ValidateRemoveResourceFromConnection`) — happy path plus at least one
  failing path per method.

Both classes share a local helper:

```csharp
private static ImmutableArray<ValidationErrorInstance> Errors(RuleExpression rule) =>
    ValidationComposer.Validate(rule)?.Errors ?? ImmutableArray<ValidationErrorInstance>.Empty;
```

which drives the rule synchronously and unpacks the resulting
`ValidationProblemInstance` into an assertable array.

## Verification

New tests run alone (xUnit v3 exe, direct):

```
-class "Altinn.AccessManagement.Enduser.Api.Tests.Validation.ConnectionCombinationRulesTest"
-class "Altinn.AccessManagement.Enduser.Api.Tests.Validation.ConnectionValidationTest"
→ Total: 53, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 1.6s
```

Full Enduser test project with coverage (Release build; Podman socket):

```powershell
$env:DOCKER_HOST = ''
$env:TESTCONTAINERS_DOCKER_SOCKET_OVERRIDE = '//./pipe/podman-machine-default'
$env:TESTCONTAINERS_RYUK_DISABLED = 'true'
./docs/testing/run-coverage.ps1 -Projects @(
  'src/apps/Altinn.AccessManagement/test/Altinn.AccessManagement.Enduser.Api.Tests/Altinn.AccessManagement.Enduser.Api.Tests.csproj'
) -NoBuild
```

| Metric | After Step 30 | After Step 31 |
|---|---|---|
| `Altinn.AccessManagement.Api.Enduser` line % | 49.93 | **62.76** |
| `Altinn.AccessManagement.Api.Enduser` branch % | 38.72 | **50.15** |
| Enduser test count (passing) | 186 | **239** |
| Enduser tests skipped | 8 | 8 |

Run time: ~74 s. No regressions — the 8 pre-existing skips are unchanged.

## Notes on the remaining ~37 % line gap

With `ConnectionValidation` and `ConnectionCombinationRules` now covered, the
biggest remaining blocks in `Api.Enduser` are:

- `Controllers.MaskinportenConsumersController` /
  `MaskinportenSuppliersController` (~55 missed lines combined) — gated by
  `POLICY_MASKINPORTEN_DELEGATION_ENDUSER_*`, which routes through a PDP
  decision via `EndUserResourceAccessHandler`. Still requires either
  stubbing PDP or seeding a delegated `altinn_maskinporten_scope_delegation`
  resource.
- `Utils.ToUuidResolver` — exercised only by the
  `ConnectionsController.AddAssignmentPerson` / `CheckResourcePerson` paths,
  which currently have no tests.
- `ParameterValidation` branches that are not composed by
  `ConnectionValidation` (e.g. `ToIsGuid`, `InstanceRightsDelegationInput`,
  `InstanceUrn`) — these are still only hit through controller tests.
  A small dedicated `ParameterValidationTest` file is a natural follow-up
  using the same `InternalsVisibleTo` plumbing added here.
- `AccessManagementEnduserHost` / `Program` startup (~34 missed) — startup
  code that is normally not counted against API coverage.

---

**Next:** The remaining Enduser work now splits into two follow-ups:
1. Direct `ParameterValidation` unit tests (same pattern, low-risk).
2. `MaskinportenConsumers/Suppliers` controller tests (needs PDP stubbing
   or delegated-resource seeding — non-trivial).

Both can use `InternalsVisibleTo` already in place.
