---
step: 17
title: AuthenticationHelper and JwtTokenUtil unit tests
phase: B
status: complete
linkedIssues:
  task: 2990
bugClassesCovered:
  - "AuthenticationHelper.GetUserId/GetPartyId/GetUserAuthenticationLevel: missing claim NREs (must return 0); non-numeric claim throws (must return 0 via int.TryParse fallback)"
  - "AuthenticationHelper.GetPartyUuid: missing claim or invalid Guid throws (must return Guid.Empty via Guid.TryParse fallback)"
  - "AuthenticationHelper.GetSystemUserUuid: deserializes 'authorization_details' JSON only when type == 'urn:altinn:systemuser' — other types must return Guid.Empty (regression: returning sysuser uuid for arbitrary auth schemas)"
  - "AuthenticationHelper.GetAuthenticatedPartyUuid composition: PartyUuid wins over SystemUserUuid when both present (regression: flipping precedence would attribute actions to the wrong principal)"
  - "AuthenticationHelper: null context.User does not NRE — null-conditional via context.User?.Claims"
  - "JwtTokenUtil.GetTokenFromContext: Bearer prefix matched case-insensitively; whitespace trimmed; no cookie + no header returns empty (no NRE)"
  - "JwtTokenUtil: empty cookie value falls through to Authorization header instead of returning empty"
verifiedTests: 26
touchedFiles: 2
---

# Step 17 — `AuthenticationHelper` + `JwtTokenUtil` unit tests

## Goal

Pin claim-extraction defaults and the JWT-token resolution chain
in
[`Altinn.AccessManagement.Core.Helpers`](../../../src/apps/Altinn.AccessManagement/src/Altinn.AccessManagement.Core/Helpers/).
These two helpers were untested before this step (`grep`
across `test/` returned only one indirect mention). The bug
classes are auth-relevant — wrong claim extraction can
mis-attribute actions to the wrong user/system-user, and broken
token extraction can silently authenticate (or fail to
authenticate) requests.

## What changed

### Tests added

[`AccessMgmt.Tests/Helpers/AuthenticationHelperTest.cs`](../../../src/apps/Altinn.AccessManagement/test/AccessMgmt.Tests/Helpers/AuthenticationHelperTest.cs)
— 19 tests covering all six accessors:

- `GetUserId` / `GetPartyId` / `GetUserAuthenticationLevel`:
  missing claim → `0`; non-numeric claim → `0` (TryParse
  fallback); valid → parsed value.
- `GetPartyUuid`: missing → `Guid.Empty`; valid → parsed; invalid
  format → `Guid.Empty`.
- `GetSystemUserUuid` / `GetSystemUserUuidString`: missing
  `authorization_details` claim → empty; valid system-user JSON →
  parsed sysuser id; non-system-user `type` → empty (silently
  ignored, doesn't throw on schema mismatch).
- `GetAuthenticatedPartyUuid` composition: `PartyUuid` wins over
  `SystemUserUuid` when both present; falls back to system-user
  when `PartyUuid` is empty; returns `Guid.Empty` when neither is
  set.
- Null `HttpContext.User` doesn't NRE (null-conditional pin).

[`AccessMgmt.Tests/Helpers/JwtTokenUtilTest.cs`](../../../src/apps/Altinn.AccessManagement/test/AccessMgmt.Tests/Helpers/JwtTokenUtilTest.cs)
— 7 tests covering `GetTokenFromContext`:

- Cookie present → cookie value returned.
- No cookie + no auth header → empty.
- No cookie + `Bearer` header → token returned without prefix.
- No cookie + `bearer` (lowercase) → also matches via
  `OrdinalIgnoreCase`.
- No cookie + `Bearer    token   ` → whitespace trimmed.
- Empty cookie value falls through to auth header.
- Non-Bearer auth header (e.g. `Basic …`) → silently dropped
  (returns null/empty — pinning current behavior).

Test harness: hand-rolled `ClaimsPrincipal` / `ClaimsIdentity`
construction with `DefaultHttpContext`. No mocking. Uses
FluentAssertions in line with the AccessMgmt.Tests house style.

## Verification

```text
$ dotnet test ...AccessMgmt.Tests... --filter-class "Helpers.AuthenticationHelperTest" --filter-class "Helpers.JwtTokenUtilTest"
Passed! - Failed: 0, Passed: 26, Skipped: 0, Total: 26, Duration: 0.5s
```

## Deferred / follow-up

- `Altinn.AccessManagement.Core.Helpers.PolicyHelper` (1067 LOC)
  and `DelegationHelper` (1194 LOC) — large helpers with mixed
  real-logic and pass-through wiring. Already partially tested;
  would need an audit pass to identify untested real-logic
  branches before adding tests.
- `RightsHelper` (218 LOC) — likely partly tested; audit pending.
- `ServiceResourceHelper` (16 LOC) — likely too thin to be worth
  isolation testing.
