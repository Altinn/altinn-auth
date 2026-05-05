---
step: 13
title: AccessMgmt.Core Authorization unit tests
phase: B
status: complete
linkedIssues:
  task: 2990
bugClassesCovered:
  - "ConditionalScope.QueryParamEquals: null HttpContext silently throws or returns true (must return false)"
  - "ConditionalScope.QueryParamEquals: missing query param treated as match (must return false)"
  - "ConditionalScope.QueryParamEquals: case-sensitive comparison (must be OrdinalIgnoreCase)"
  - "ScopeConditionAuthorizationHandler: predicate-true-but-scope-missing wrongly Succeeds (must require scope)"
  - "ScopeConditionAuthorizationHandler: predicate-false-but-scope-present wrongly Succeeds (must require predicate match)"
  - "ScopeConditionAuthorizationHandler: empty access list silently Succeeds"
  - "ScopeConditionAuthorizationHandler: continues evaluating rules after first match — wastes work and could fire side effects"
  - "ScopeConditionAuthorizationHandler: only first ConditionalScope evaluated; later rules never matched"
  - "DefaultAuthorizationScopeProvider: yields urn:altinn:scope claims from non-Federation identities (must filter to Federation)"
  - "DefaultAuthorizationScopeProvider: top-level scope claim treated as a single scope string (must split on space)"
  - "DefaultAuthorizationScopeProvider: empty space-separated entries yielded as empty scopes"
verifiedTests: 21
touchedFiles: 3
---

# Step 13 — `AccessMgmt.Core.Authorization` unit tests

## Goal

Pin down auth-path behavior in
[`Altinn.AccessMgmt.Core.Authorization`](../../../src/apps/Altinn.AccessManagement/src/Altinn.AccessMgmt.Core/Authorization/).
The three classes were untested (grep across `test/` returned 0
hits before this step) — high-leverage gap because auth bugs map
directly to security-relevant bug classes.

## What changed

### Tests added

[`Authorization/ConditionalScopeTest.cs`](../../../src/apps/Altinn.AccessManagement/test/Altinn.AccessMgmt.Core.Tests/Authorization/ConditionalScopeTest.cs)
— 8 tests for `ConditionalScope.QueryParamEquals` and the
`ToOthers` / `FromOthers` factory predicates. Test harness uses
`new DefaultHttpContext()` + a `StubAccessor : IHttpContextAccessor`.

[`Authorization/ScopeConditionAuthorizationHandlerTest.cs`](../../../src/apps/Altinn.AccessManagement/test/Altinn.AccessMgmt.Core.Tests/Authorization/ScopeConditionAuthorizationHandlerTest.cs)
— 6 tests for `ScopeConditionAuthorizationHandler.HandleAsync`
covering the predicate × scope matrix and the early-exit on first
match. `ScopeConditionAuthorizationRequirement` has an internal
constructor; tests build it via `Activator.CreateInstance` with
non-public binding.

[`Authorization/DefaultAuthorizationScopeProviderTest.cs`](../../../src/apps/Altinn.AccessManagement/test/Altinn.AccessMgmt.Core.Tests/Authorization/DefaultAuthorizationScopeProviderTest.cs)
— 7 tests for the `internal sealed`
`DefaultAuthorizationScopeProvider.GetScopeStrings` (visible to
the test project via the auto-`InternalsVisibleTo` rule). Pins
both source branches: federation-identity `urn:altinn:scope`
claims (one scope per claim) and top-level "scope" claims
(space-separated, split + `RemoveEmptyEntries`).

## Verification

```text
$ dotnet test ...Altinn.AccessMgmt.Core.Tests... --filter-namespace "Altinn.AccessMgmt.Core.Tests.Authorization"
Passed! - Failed: 0, Passed: 21, Skipped: 0, Total: 21, Duration: 3s

$ dotnet test ...Altinn.AccessMgmt.Core.Tests...
Passed! - Failed: 0, Passed: 363, Skipped: 0, Total: 363, Duration: 45s
```

363 = 342 prior + 21 here. No regressions.

## Deferred / follow-up

- `Altinn.AccessMgmt.Core.Authorization.AuthorizationRequirementsExtensions`
  builders — fluent-API surface that constructs the public
  `ScopeConditionAuthorizationRequirement` via the internal
  constructor. Pass-through wiring; skip unless a real bug class
  emerges.
