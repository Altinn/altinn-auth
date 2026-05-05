---
step: 22
title: Api.Enduser DecisionHelper unit tests
phase: B
status: complete
linkedIssues:
  task: 2990
bugClassesCovered:
  - "GetUserPartyUuid / GetUserId: missing claim or invalid format throws (must default safely)"
  - "GetFromParam / GetToParam / GetPartyParam: missing or non-Guid querystring throws (must return null)"
  - "ValidatePdpDecision: null response or null user silently returns false (must throw ArgumentNullException so the bug is loud)"
  - "ValidatePdpDecision: zero or multi-result response treated as Permit (must return false)"
  - "ValidatePdpDecision: minimum-authentication-level obligation ignored — under-authenticated user gets Permit (auth bypass)"
  - "ValidatePdpDecision: minimum-authentication-level obligation rejects authenticated user above threshold (denial-of-service)"
verifiedTests: 20
touchedFiles: 1
---

# Step 22 — Api.Enduser `DecisionHelper` unit tests

Pin the auth-decision validation in
[`DecisionHelper`](../../../src/apps/Altinn.AccessManagement/src/Altinn.AccessManagement.Api.Enduser/Authorization/Helper/DecisionHelper.cs)
— the helper that translates PDP responses into permit/deny
decisions for the Enduser API. Auth-relevant; a regression that
mishandles obligations or the multi-result guard could silently
authorize unauthorized requests.

## Tests

[`Altinn.AccessManagement.Enduser.Api.Tests/AuthorizationHelpers/DecisionHelperTest.cs`](../../../src/apps/Altinn.AccessManagement/test/Altinn.AccessManagement.Enduser.Api.Tests/AuthorizationHelpers/DecisionHelperTest.cs)
— 20 tests:

- Claim accessors (`GetUserPartyUuid`, `GetUserId`) — missing /
  valid / invalid claim defaults.
- Querystring accessors (`GetFromParam`, `GetToParam`,
  `GetPartyParam`) — valid Guid / missing / non-Guid → null.
- `ValidatePdpDecision`: null response throws; null user throws;
  zero results → false; multi-result → false; permit + no
  obligations → true; non-permit → false; permit with
  minimum-auth-level obligation, user below → false; user above
  → true.

Test harness uses `DefaultHttpContext` for the HttpContext-based
accessors and hand-constructed `XacmlJsonResponse` /
`XacmlJsonObligationOrAdvice` graphs for the validation tests. No
mocking.

Note: the test file lives under `AuthorizationHelpers/` rather
than `Authorization/` — a sibling test namespace named
`Altinn.AccessManagement.Enduser.Api.Tests.Authorization` collides
with the production `Altinn.AccessManagement.Api.Enduser.Authorization`
namespace at compile time when other test files reference the
production namespace, breaking unrelated test files in the same
project.

## Verification

```text
20/20 pass; ~4s.
```
