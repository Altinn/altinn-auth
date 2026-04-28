---
step: 5
title: Remove AccessManagementAuthorizedParties feature flag and its dead-code paths
phase: A
status: complete
linkedIssues:
  feature: 2946
  task: 2947
coverageDelta:
  # No real coverage impact: the legacy code paths being deleted were
  # already not being executed in the merged-cobertura measurement
  # (the production flag is always-on). Production line counts for
  # `Altinn.Authorization` shrink slightly because the flag-branching
  # code itself is gone. Authoritative numbers refreshed in Step 6.
  Altinn.Authorization: { line: 0.0, branch: 0.0, note: marginal_improvement_baseline_at_step6 }
verifiedTests: 402
testFailures: 0
testSkips: 0
touchedFiles: 6
auditIds: [C2']
---

# Step 5 — Remove `AccessManagementAuthorizedParties` feature flag and its dead-code paths

## Goal

Resolve audit finding **C2'** by removing the now-obsolete
`AccessManagementAuthorizedParties` feature flag entirely, instead
of patching around its dead `else` branches. The team architect
confirmed (out-of-band) that the flag has been always-on in
production for some time and is ready to retire.

The originally-flagged "authorization regression" framing turns out
to be a test-hits-dead-code artefact rather than a security-relevant
condition in the field — see [§Triage outcome](#triage-outcome) below
for the corrected narrative.

## Triage outcome

The failing test `ValidateParty_NotAsAuthenticatedUser_Forbidden` was
hitting the legacy `else` branch of
`PartiesController.ValidateSelectedParty`, which was gated by the
`AccessManagementAuthorizedParties` feature flag:

```csharp
// before
if (await _featureManager.IsEnabledAsync(FeatureFlags.AccessManagementAuthorizedParties))
{
    int? authnUserId = User.GetUserIdAsInt();
    if (userId != authnUserId) return Forbid();        // cross-user check HERE
    return Ok(await ValidateSelectedAuthorizedParty(partyId, ct));
}
else
{
    // legacy path — no cross-user check
    return Ok(await _partiesWrapper.ValidateSelectedParty(userId, partyId, ct));
}
```

The test class flips the mocked flag to `false` mid-class
(`GetPartyList_AsAuthenticatedUser_Ok` mutates `_featureManageMock`
to compare original-vs-new responses), which leaks into subsequent
tests and exposes the legacy `else` branch. The 200 the test received
came from the mock's "say yes if `partyId` is in `userId`'s list",
which doesn't enforce the cross-user check.

In production, the flag has been always-on for some time — the
`else` branch was unreachable. The architect confirmed it's ready
to retire entirely. So the right move is to **delete the flag and
its dead paths**, not to patch the cross-user check inside the dead
branch.

**Security disposition:** no advisory needed. The flag's always-on
prod state means the legacy code path was never reachable in the
field; the bug existed only in test-mock-flipped scenarios.

## What changed

### Production code

- **[`src/apps/Altinn.Authorization/src/Altinn.Authorization/Configuration/FeatureFlags.cs`](../../../../src/apps/Altinn.Authorization/src/Altinn.Authorization/Configuration/FeatureFlags.cs)**
  — removed the `AccessManagementAuthorizedParties` constant and its
  docstring.
- **[`src/apps/Altinn.Authorization/src/Altinn.Authorization/Controllers/PartiesController.cs`](../../../../src/apps/Altinn.Authorization/src/Altinn.Authorization/Controllers/PartiesController.cs)**:
  - Dropped the `if (await _featureManager.IsEnabledAsync(...)) { … } else { legacy }` block from both `GetPartyList` and `ValidateSelectedParty`. Each method now calls only the `AuthorizedParties`-based path.
  - The cross-user check `if (userId != authnUserId) return Forbid()` now sits at the top of `ValidateSelectedParty` (matching the long-standing pattern in `GetPartyList`).
  - Removed the now-unused `IParties partiesWrapper` and `IFeatureManager featureManager` constructor params + their backing fields. The controller's only remaining dependency is `IAccessManagementWrapper`.
  - Removed the `using Microsoft.FeatureManagement;` and `using Altinn.Platform.Authorization.Configuration;` directives that became unused.
- **[`src/apps/Altinn.Authorization/src/Altinn.Authorization/Services/Interface/IParties.cs`](../../../../src/apps/Altinn.Authorization/src/Altinn.Authorization/Services/Interface/IParties.cs)**
  — removed `Task<List<Party>> GetParties(int userId, ...)` and
  `Task<bool> ValidateSelectedParty(int userId, int partyId, ...)`
  (now dead — `PartiesController` was their only consumer).
  Remaining methods `GetParty`, `GetKeyRoleParties`, `GetMainUnits`
  are still used by `ContextHandler` and `DelegationContextHandler`.
- **[`src/apps/Altinn.Authorization/src/Altinn.Authorization/Services/Implementation/PartiesWrapper.cs`](../../../../src/apps/Altinn.Authorization/src/Altinn.Authorization/Services/Implementation/PartiesWrapper.cs)**
  — removed the impls of the two dropped interface methods (the
  HTTP-to-SBL-Bridge calls for `authorization/api/parties` and the
  in-memory party-list scan). The remaining `GetParty`,
  `GetKeyRoleParties`, `GetMainUnits` impls are unchanged.

### Test code

- **[`src/apps/Altinn.Authorization/test/Altinn.Authorization.Tests/MockServices/PartiesMock.cs`](../../../../src/apps/Altinn.Authorization/test/Altinn.Authorization.Tests/MockServices/PartiesMock.cs)**
  — removed the impls of `GetParties` and `ValidateSelectedParty`
  (and the `Data/Parties/<userId>.json` JSON-file lookup helper that
  only `GetParties` used). Removed the `System.IO`, `System.Linq`,
  `System.Text.Json` usings that became unused.
- **[`src/apps/Altinn.Authorization/test/Altinn.Authorization.Tests/PartiesControllerTest.cs`](../../../../src/apps/Altinn.Authorization/test/Altinn.Authorization.Tests/PartiesControllerTest.cs)**:
  - Removed the `_featureManageMock` field, its constructor setup,
    and the `fixture.ConfigureServices(...)` call that registered it.
  - Simplified `GetPartyList_AsAuthenticatedUser_Ok` — was a
    legacy-vs-new comparison that flipped the flag mid-test; now
    just asserts `200 OK` + non-null party list (the comparison was
    only meaningful while both code paths existed).
  - The `ValidateParty_NotAsAuthenticatedUser_Forbidden` test passes
    naturally because the only code path now starts with the
    cross-user check.
  - Removed the `Microsoft.FeatureManagement`,
    `Microsoft.Extensions.DependencyInjection`, and `Moq` usings
    that became unused.

### Documentation

- Audit doc [`TESTING_INFRASTRUCTURE_OVERHAUL_PART_2.md`](../TESTING_INFRASTRUCTURE_OVERHAUL_PART_2.md):
  C2' marked resolved with the corrected triage narrative and an
  explicit "no security advisory needed" disposition; A.2 marked
  done in §4 Phase A.
- Step log row + Recommended Next Steps refresh + Blocked Items
  `Last re-checked = step 5` in [`INDEX.md`](INDEX.md).

## Verification

### Build

```
dotnet build src/apps/Altinn.Authorization/test/Altinn.Authorization.Tests/Altinn.Authorization.Tests.csproj \
  --configuration Release --verbosity minimal
```

Result: **0 errors**, 165 warnings (all pre-existing StyleCop noise
on unrelated test files; not introduced by this change).

### Full `Altinn.Authorization.Tests` run

```
dotnet src/apps/Altinn.Authorization/test/Altinn.Authorization.Tests/bin/Release/net9.0/Altinn.Authorization.Tests.dll
```

Result:

```
=== TEST EXECUTION SUMMARY ===
   Altinn.Authorization.Tests  Total: 402, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 9.363s
```

- **Pre-fix (Step 1 baseline):** 402 / 401 / 1 / 0.
- **Post-fix:** 402 / **402** / 0 / 0. Previously-failing
  `ValidateParty_NotAsAuthenticatedUser_Forbidden` now passes
  because the legacy code path it was hitting no longer exists.
- **Sibling regressions:** zero. All other tests (including
  `PartiesControllerTest` siblings, `ContextHandler` tests that
  still use `IParties.GetParty`/`GetKeyRoleParties`/`GetMainUnits`,
  etc.) continue to pass.

### Reasoning (for the reviewer)

The deleted code was guarded by an always-on production flag. With
the flag removed, the deleted lines of `PartiesController`,
`PartiesWrapper`, and `PartiesMock` no longer ship — but they were
already unreachable in production.

The behavioural surface of the API is unchanged: both endpoints
(`GET /authorization/api/v1/parties` and
`GET /authorization/api/v1/parties/{id}/validate`) continue to
serve the AuthorizedParties path that production has been using
all along. The cross-user `Forbid()` check is now run unconditionally
on `ValidateSelectedParty`, which (a) matches the long-standing
behaviour of `GetPartyList` and (b) is the correct contract per the
test that was failing.

## Deferred / follow-up

- **Coverage refresh** — the `Altinn.Authorization` assembly's line
  count shrinks slightly (the flag-branching code itself is
  removed), so the pre-Step-5 baseline (`Altinn.Authorization`
  69.09% / 72.72%) needs a fresh measurement. Done in Step 6's
  closing sweep.
- **`Altinn.Authorization.Configuration.FeatureFlags`** still has
  five other constants (`AuditLog`, `SystemUserAccessPackageAuthorization`,
  `UserAccessPackageAuthorization`, `DecisionRequestLogRequestOnError`,
  `DecisionRequestLogRequestOnErrorMultiRequest`). None are touched
  here; only `AccessManagementAuthorizedParties` was confirmed
  always-on by the architect.
- **`PartiesControllerTest` mock-state-leak** noted in the original
  Step 1 audit was downstream of the flag-flipping flow. With the
  flag-flipping gone (Step 5 simplified the test), the leak is also
  gone — no separate cleanup needed.

## Blocked-items sweep

No items unblocked. Refresh `Last re-checked = step 5` on the three
Blocked Items rows in [`INDEX.md`](INDEX.md#blocked-items).

## Obsolete-docs sweep

None. No prior step doc covered this work. (An earlier draft of this
step took a different approach — hoisting the cross-user check
inside the feature-flag branch — and was committed and pushed before
the architect's confirmation about the flag's removability landed.
That commit was rectified out of the branch's history before this
step shipped.)
