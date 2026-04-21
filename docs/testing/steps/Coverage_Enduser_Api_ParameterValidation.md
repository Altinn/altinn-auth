# Step 32 — Coverage: AccessManagement.Api.Enduser `ParameterValidation` (Phase 6.7c continued)

## Goal

Close another slice of the `Altinn.AccessManagement.Api.Enduser` coverage gap
by direct-unit-testing the atomic per-parameter validation rules in
`ParameterValidation`. Step 31 noted that several branches of this class are
not composed by `ConnectionValidation` (in particular `ToIsGuid`,
`InstanceRightsDelegationInput`, `InstanceUrn`, and the keyword handling on
`Party`/`PartyFrom`/`PartyTo`) and are therefore only exercised — if at all —
through expensive controller tests.

## What changed

### New test file

`test/Altinn.AccessManagement.Enduser.Api.Tests/Validation/ParameterValidationTest.cs`
— **44 `[Fact]`s**, mirroring the pattern established in Step 31
(`ConnectionCombinationRulesTest` / `ConnectionValidationTest`): rules are
driven through `ValidationComposer.Validate(...)`, errors unpacked into an
`ImmutableArray<ValidationErrorInstance>`, and assertions use
`Paths[0].Should().Be("$QUERY/...")` (the path retains the leading `$` unlike
some of the rules covered in Step 31 that build paths via a different
overload).

Coverage per method:

| Method | Cases covered |
|---|---|
| `Party(string)` | valid GUID, keyword `me` (+ case-insensitive), keyword `all` (rejected — only valid for from/to), null/empty/Guid.Empty/malformed (all error) |
| `ToIsGuid(Guid?, string)` | valid GUID, null (error), `Guid.Empty` (error), custom `paramName` reflected in path |
| `PartyFrom(string, string)` | valid GUID, keywords `me` and `all` (+ case-insensitive), `Guid.Empty`, malformed, custom `paramName` |
| `PartyTo(string, string)` | valid GUID, keyword `all`, malformed, custom `paramName` |
| `PersonInput(string, string)` | valid, null/empty/whitespace identifier, null/empty/whitespace last name |
| `InstanceRightsDelegationInput(Guid?, PersonInputDto?, IEnumerable<string>?)` | only-`to`, only-`toInput`, both (conflict), neither, `Guid.Empty`, null `directRightKeys`, empty `directRightKeys` |
| `InstanceUrn(string)` | null/empty/whitespace (all allowed), each of the three valid prefixes (Apps/Correspondence/Dialog, + case-insensitive Apps), invalid prefix, arbitrary string |

Reuses the same `Errors(RuleExpression)` helper as Step 31. The
`InternalsVisibleTo` item added in Step 31 gives the test assembly direct
access to the internal `ParameterValidation` class, so no production-code
changes were required.

## Verification

New tests run in isolation:

```
Altinn.AccessManagement.Enduser.Api.Tests.Validation.ParameterValidationTest
→ 44 passed, 0 failed, 0 skipped, 645 ms
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

| Metric | After Step 31 | After Step 32 |
|---|---|---|
| `Altinn.AccessManagement.Api.Enduser` line % | 62.76 | **65.94** |
| `Altinn.AccessManagement.Api.Enduser` branch % | 50.15 | **55.49** |
| Enduser test count (added in this step) | — | **+44** |

The full-project run also surfaced 18 pre-existing integration-test failures
(flaky / environment-dependent controller tests that were already
intermittently failing in the Step 31 baseline run — they are independent of
the new unit tests, all 44 of which pass deterministically).

## Remaining gap

With `ParameterValidation` now covered, the next-largest blocks in
`Api.Enduser` continue to be the ones flagged at the end of Step 31:

- `Controllers.MaskinportenConsumersController` /
  `MaskinportenSuppliersController` — gated by
  `POLICY_MASKINPORTEN_DELEGATION_ENDUSER_*` (PDP decision via
  `EndUserResourceAccessHandler`); still needs PDP stubbing or seeding of the
  `altinn_maskinporten_scope_delegation` delegated resource.
- `Utils.ToUuidResolver` — only reachable through
  `ConnectionsController.AddAssignmentPerson` / `CheckResourcePerson`.
- `AccessManagementEnduserHost` / `Program` startup — typically excluded from
  API coverage expectations.

---

**Next:** The two remaining Enduser follow-ups (Maskinporten controllers,
`ToUuidResolver`) require controller-level integration tests with PDP
stubbing. A step to audit the next-best coverage target (AccessMgmt
persistence layers, 6.7d) is likely a higher-priority use of a fresh chat.
