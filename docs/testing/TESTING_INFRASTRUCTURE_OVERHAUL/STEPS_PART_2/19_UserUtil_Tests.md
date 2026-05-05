---
step: 19
title: Api.Internal UserUtil unit tests
phase: B
status: complete
linkedIssues:
  task: 2990
bugClassesCovered:
  - "GetUserUuid: null principal NREs (must return null)"
  - "GetUserUuid: missing party-uuid claim returns Guid.Empty (regression: must return null instead, callers distinguish 'not present' from 'present but empty')"
  - "GetUserUuid: invalid Guid value throws (must return null via TryParse fallback)"
verifiedTests: 5
touchedFiles: 1
---

# Step 19 — Api.Internal `UserUtil` unit tests

Pin null/invalid-input defaults on
[`UserUtil.GetUserUuid`](../../../src/apps/Altinn.AccessManagement/src/Altinn.AccessManagement.Api.Internal/Utils/UserUtil.cs).
Auth-relevant — the helper extracts `urn:altinn:party:uuid` to
identify the calling user; a regression that returned
`Guid.Empty` instead of `null` would silently misattribute actions.

## Tests

[`Api.Tests/Internal/UserUtilTest.cs`](../../../src/apps/Altinn.AccessManagement/test/Altinn.AccessManagement.Api.Tests/Internal/UserUtilTest.cs)
— 5 tests covering null principal, no claims, no matching claim,
invalid Guid value, and the happy path.

## Verification

```text
5/5 pass; ~1s.
```

## Deferred

- Enterprise `OrgUtil` (a duplicate of `Altinn.AccessMgmt.Core.Utils.OrgUtil`
  which is already tested) — should converge under the
  mock-consolidation Task rather than be tested twice.
- Enterprise `ModelMapper` URN switch dispatch — would need
  setting up `ConsentPartyUrn` external/internal type pairs.
