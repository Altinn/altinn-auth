# Step 7 — Maximize Code Coverage (Phase 6)

## Goal

Increase test coverage across all source projects, starting with the
highest-impact, lowest-effort gaps identified in the Phase 5 baseline.

## Sub-step 6.3: PEP Coverage Sprint

### Baseline → After

| Assembly | Line% Before | Line% After | Branch% Before | Branch% After |
|---|---|---|---|---|
| Altinn.Authorization.PEP (own tests) | 60.20 | **78.99** | 56.25 | **78.68** |

### New Test Files

| File | Tests | Covers |
|---|---|---|
| `ClaimAccessHandlerTest.cs` | 6 | `ClaimAccessHandler.HandleRequirementAsync` — matching claim, no match, no claims, wrong value, multiple claims, null user claims |
| `IDFormatDeterminatorTest.cs` | 18 | `DetermineIDFormat`, `IsValidOrganizationNumber`, `IsValidSSN`, `IsValidUserName` — null, empty, valid, invalid, edge cases |
| `PDPAppSITest.cs` | 4 | `IPDP.GetDecisionForRequest` and `GetDecisionForUnvalidateRequest` — valid response, null response, permit, exception |
| `DecisionHelperTest.cs` (additions) | 24 | `ValidatePdpDecisionWithoutObligationCheck`, `ValidatePdpDecisionDetailed`, org-level obligations in `ValidateDecisionResult`, `CreateDecisionRequestForResourceRegistryResource`, consumer claims, system user claims, sid/jti claims, person UUID, party ID, organization number attribute override, `CreateActionCategory` with includeResult, `CreateXacmlJsonAttribute` |

**Total new tests: 52** (40 → 92)

### Approach

- Focused on untested classes first (`ClaimAccessHandler`, `IDFormatDeterminator`)
- Then expanded `DecisionHelper` coverage for uncovered claim-type branches and
  validation methods (`ValidatePdpDecisionWithoutObligationCheck`, detailed results)
- `PDPAppSI` tested via `IPDP` mock (the class depends on `AuthorizationApiClient`
  which has no interface — full integration test deferred)
- `AuthorizationApiClient` coverage deferred (requires HTTP test server setup)

## Verification

- [x] Build passes (0 errors)
- [x] All 92 PEP tests pass
- [x] PEP line coverage ≥ 75% target

## Full Baseline (Phase 5 + current)

| Assembly | Line% | Branch% | Source |
|---|---|---|---|
| Altinn.Authorization | 62.77 | 63.50 | Authorization.Tests |
| Altinn.Authorization.ABAC | 62.91 | 61.29 | Authorization.Tests |
| **Altinn.Authorization.PEP** | **78.99** | **78.68** | PEP.Tests |
| Altinn.Authorization.Host.Lease | 0 | 0 | Host.Lease.Tests (2 skipped — need storage account) |

> AccessManagement projects require Docker (Testcontainers) — baseline deferred.

## Sub-step 6.4: Authorization.Tests Edge-Case Coverage Sprint

### Baseline → After

| Assembly | Line% Before | Line% After | Branch% Before | Branch% After |
|---|---|---|---|---|
| Altinn.Authorization | 62.77 | _TBD (run coverage)_ | 63.50 | _TBD_ |
| Altinn.Authorization.ABAC | 62.91 | _TBD_ | 61.29 | _TBD_ |

> Coverage numbers pending `run-coverage.ps1` execution (no Docker needed for these projects).

### New Test Files

| File | Tests | Covers |
|---|---|---|
| `ExtensionTests/StringExtensionsTest.cs` | 8 | `StringExtensions.AsFileName` — null, empty, whitespace, valid chars, invalid chars throw vs replace, mixed chars |
| `ExtensionTests/ClaimsPrincipalExtensionsTest.cs` | 16 | `GetUserOrOrgId` (4), `GetOrg` (2), `GetOrgNumber` (3), `GetUserIdAsInt` (3), `GetAuthenticationLevel` (3) |
| `EventLogHelperTest.cs` | 10 | `GetResourceAttributes` (3), `GetSubjectInformation` (3), `GetActionInformation` (3), `GetClientIpAddress` (1) |
| `PolicyHelperTest.cs` | 16 | `GetAltinnAppsPolicyPath` (3), `GetAltinnAppDelegationPolicyPath` (5), `GetPolicyResourceType` (4), `GetPolicyPath`, `GetRolesWithAccess` (2), `BuildDelegationPolicy`, `ParsePolicy`, `GetMinimumAuthenticationLevelFromXacmlPolicy` |
| `DelegationHelperAdditionalTest.cs` | 16 | `TryGetCoveredByPartyIdFromMatch` (5), `TryGetCoveredByUserIdFromMatch` (2), `GetCoveredByFromMatch` (3), `TryGetResourceFromAttributeMatch` (2), `GetAttributeMatchKey`, `GetPolicyCount` (2), `GetRulesCountToDeleteFromRequestToDelete`, `TryGetDelegationParamsFromRule` (2), `SetRuleType` (4) |

**Total new tests: 66** (218 → 284)

### Approach

- Targeted previously **completely untested** classes first: `EventLogHelper`, `ClaimsPrincipalExtensions`, `StringExtensions`, `PolicyHelper`.
- Extended `DelegationHelper` coverage for methods not reached by existing `DelegationHelperTest` (10 existing tests cover `SortRulesByDelegationPolicyPath` and `PolicyContainsMatchingRule` only).
- All tests are pure unit tests — no WAF or Testcontainers needed.

### Verification

- [x] Build passes (0 errors)
- [x] All 284 Authorization.Tests pass (218 existing + 66 new)

---

## Remaining Sub-steps

| Sub-step | Status | Notes |
|---|---|---|
| 6.1 Full baseline across all 11 projects | ⬜ | Needs Docker for AccessMgmt projects |
| 6.2 Triage uncovered business logic | ⬜ | |
| **6.3 PEP coverage sprint** | **✅** | 60% → 79% |
| **6.4 Authorization.Tests edge cases** | **✅** | +66 tests (218 → 284) |
| 6.5 Host.Lease tests | ⬜ | Blocked by storage account dependency |
| **6.6 CI threshold** | **✅** | Per-assembly thresholds in CI via `coverage-thresholds.json` |
| 6.7 AccessManagement coverage | ⬜ | |

## Sub-step 6.7a: Authorization Infrastructure & Utility Coverage Sprint

### New Test Files

| File | Tests | Covers |
|---|---|---|
| `ExtensionTests/HttpClientExtensionTest.cs` | 9 | `PostAsync` (5) — no tokens, auth token, platform token, both tokens, POST method; `GetAsync` (4) — no tokens, auth token, platform token, GET method |
| `PlatformHttpExceptionTest.cs` | 4 | `CreateAsync` message formatting, `CreateAsync` stores response, constructor, `IsException` inheritance |
| `ServiceResourceHelperTest.cs` | 11 | `ResourceIdentifierRegex` — valid (5: alpha, alphanumeric, hyphen, underscore, mixed), invalid (6: too short, empty, uppercase, space, dot, special char) |
| `XacmlRequestApiModelBinderTest.cs` | 3 | `BindModelAsync` — null context throws, valid body, empty body |
| `XacmlRequestApiModelBinderProviderTest.cs` | 3 | `GetBinder` — null context throws, matching type returns binder, non-matching type returns null |

**Total new tests: 30** (284 → 314)

### Approach

- Targeted **previously completely untested** classes: `HttpClientExtension`, `PlatformHttpException`,
  `ServiceResourceHelper`, `XacmlRequestApiModelBinder`, `XacmlRequestApiModelBinderProvider`.
- All tests are pure unit tests — no WAF, Docker, or external dependencies needed.
- Used `RecordingHandler` (test double for `HttpMessageHandler`) to verify HTTP extension behavior.
- Used `DefaultHttpContext` and `DefaultModelBindingContext` for model binder tests.

### Verification

- [x] Build passes (0 errors)
- [x] All 314 Authorization.Tests pass (284 existing + 30 new)

---

## Remaining Sub-steps

| Sub-step | Status | Notes |
|---|---|---|
| 6.1 Full baseline across all 11 projects | ⬜ | Needs Docker for AccessMgmt projects |
| 6.2 Triage uncovered business logic | ⬜ | |
| **6.3 PEP coverage sprint** | **✅** | 60% → 79% |
| **6.4 Authorization.Tests edge cases** | **✅** | +66 tests (218 → 284) |
| 6.5 Host.Lease tests | ⬜ | Blocked by storage account dependency |
| **6.6 CI threshold** | **✅** | Per-assembly thresholds in CI via `coverage-thresholds.json` |
| **6.7a Authorization infra/utility coverage** | **✅** | +30 tests (284 → 314) |
| **6.7b Authorization models & services coverage** | **✅** | +23 tests (314 → 337) |
| 6.7c AccessManagement coverage | ⬜ | Needs Docker |

## Sub-step 6.7b: Authorization Models & Services Coverage Sprint

### New / Updated Test Files

| File | Tests | Covers |
|---|---|---|
| `OrganizationNumberTest.cs` (new) | 18 | `Parse` (4: string valid/invalid, span valid/invalid), `TryParse` (6: valid, too short, too long, letters, null, span), `CreateUnchecked` (1), `ToString` overloads (2), `TryFormat` (2: sufficient/insufficient buffer), JSON round-trip (1), JSON invalid (1), `GetExamples` (1) |
| `EventLogServiceTest.cs` (new) | 2 | `CreateAuthorizationEvent` — audit log enabled enqueues event, audit log disabled does not enqueue |
| `EventLogHelperTest.cs` (updated) | 3 new | `GetClientIpAddress` with forwarded-for header, no header; `MapAuthorizationEventFromContextRequest` full field mapping |

**Total new tests: 23** (314 → 337)

### Approach

- `OrganizationNumber` — previously completely untested model with substantial parsing, formatting,
  and JSON serialization logic (~140 lines). Tests cover all public API surface.
- `EventLogService` — previously untested service. Tested with mock `IFeatureManager` and
  `IEventsQueueClient` to verify audit-log feature flag gating.
- `MapAuthorizationEventFromContextRequest` — the main orchestrating method in `EventLogHelper`
  that was missing from the existing test suite.

### Verification

- [x] Build passes (0 errors)
- [x] All 337 Authorization.Tests pass (314 existing + 23 new)

---

## Sub-step 6.7c: Authorization Service-Layer Coverage Sprint

> **Historical note:** After 6.7b, the next candidates considered were: 6.1 (full baseline), 6.5 (Host.Lease), additional service-layer coverage (`ContextHandler`, `DelegationContextHandler`, `PolicyInformationPoint`, `AccessListAuthorization`), or returning to deferred work. The team proceeded with service-layer coverage (6.7c-6.7f).

### New Test Files

| File | Tests | Covers |
|---|---|---|
| `PolicyInformationPointTest.cs` | 7 | `GetRulesAsync` — no delegations returns empty, RevokeLast skipped, Grant extracts rules from XACML policy, Deny rules excluded, null-target rules excluded, mixed delegation types, multiple permit rules in single policy |

**Total new tests: 7** (337 → 344)

### Approach

- Targeted `PolicyInformationPoint` — previously completely untested service with non-trivial
  rule extraction logic (filters delegation changes by type, parses XACML policy rules,
  maps action/subject/resource attribute matches to `Rule` model).
- All tests are pure unit tests using Moq for `IPolicyRetrievalPoint` and `IDelegationMetadataRepository`.

### Verification

- [x] Build passes (0 errors)
- [x] All 344 Authorization.Tests pass (337 existing + 7 new)

---

## Remaining Sub-steps

| Sub-step | Status | Notes |
|---|---|---|
| 6.1 Full baseline across all 11 projects | ⬜ | Needs Docker for AccessMgmt projects |
| 6.2 Triage uncovered business logic | ⬜ | |
| **6.3 PEP coverage sprint** | **✅** | 60% → 79% |
| **6.4 Authorization.Tests edge cases** | **✅** | +66 tests (218 → 284) |
| 6.5 Host.Lease tests | ⬜ | Blocked by storage account dependency |
| **6.6 CI threshold** | **✅** | Per-assembly thresholds in CI via `coverage-thresholds.json` |
| **6.7a Authorization infra/utility coverage** | **✅** | +30 tests (284 → 314) |
| **6.7b Authorization models & services coverage** | **✅** | +23 tests (314 → 337) |
| **6.7c Authorization service-layer coverage** | **✅** | +7 tests (337 → 344) |
| **6.7d AccessListAuthorization coverage** | **✅** | +5 tests (344 → 349) |
| **6.7e DelegationContextHandler coverage** | **✅** | +18 tests (349 → 367) |
| **6.7f ContextHandler unit tests** | **✅** | +35 tests (367 → 402) |
| 6.7g AccessManagement coverage | ⬜ | Needs Docker |

## Sub-step 6.7d: AccessListAuthorization Coverage Sprint

### New Test Files

| File | Tests | Covers |
|---|---|---|
| `AccessListAuthorizationTest.cs` | 5 | `Authorize` — null memberships → NotAuthorized, empty memberships → NotAuthorized, null ActionFilters → Authorized (wildcard), matching action filter → Authorized, non-matching action filter → NotAuthorized |

**Total new tests: 5** (344 → 349)

### Approach

- `AccessListAuthorization` — previously completely untested service with 3 distinct authorization
  branches based on access list membership and action filter matching.
- Uses JSON deserialization to construct `AccessListAuthorizationRequest` (complex URN types
  `UrnJsonTypeValue<PartyUrn>`, `UrnJsonTypeValue<ResourceIdUrn>`, `UrnJsonTypeValue<ActionUrn>`).
- Single mock dependency (`IResourceRegistry`), all tests are pure unit tests.

### Verification

- [x] Build passes (0 errors)
- [x] All 349 Authorization.Tests pass (344 existing + 5 new)

---

## Sub-step 6.7e: DelegationContextHandler Coverage Sprint

### New Test Files

| File | Tests | Covers |
|---|---|---|
| `DelegationContextHandlerTest.cs` | 18 | `GetSubjectUserId` (2: present/absent), `GetSubjectPartyId` (2: present/absent), `GetSubjectAttributeMatch` (3: first priority, fallback, no match), `GetActionString` (2: present/absent), `GetResourceAttributes` (2: org+app, instance values), `EnrichRequestSubjectAttributes` (7: user+instance adds person uuid, user non-instance skips uuid, user with keyrole parties, party person instance, party org instance, party non-instance skips, no subject does nothing) |

**Total new tests: 18** (349 → 367)

### Approach

- `DelegationContextHandler` — previously completely untested service extending `ContextHandler` with
  12 constructor dependencies. All sync methods are pure attribute-extraction logic.
  `EnrichRequestSubjectAttributes` has 7 async branches tested with mocked `IProfile`,
  `IParties`, and `IRegisterService`.
- Used `MemoryCache` (real) to satisfy base class caching.

### Verification

- [x] Build passes (0 errors)
- [x] All 367 Authorization.Tests pass (349 existing + 18 new)

---

## Sub-step 6.7f: ContextHandler Unit-Test Coverage Sprint

### New Test Files

| File | Tests | Covers |
|---|---|---|
| `ContextHandlerUnitTest.cs` | 35 | `GetResourceAttributeValues` (16: org, app, instance, task, resource-party, resource-registry, app-resource, org-number, person-uuid, party-uuid, all-attributes, unknown-attribute, empty, org-only, app-only, multiple-values), `AddIfValueDoesNotExist` (2: new/existing), `GetAttribute` (2: found/missing), `EnrichResourceParty` (5: org-number, person-id, party-uuid, missing-party, no-identifiers), cached lookups (6: GetRoles, GetUserProfileByUserId, GetUserProfileByPersonId, GetKeyRolePartyIds, GetMainUnits, GetOedRoleAssignments), attribute builders (4: GetRoleAttribute, GetPartyTypeAttribute person/org, GetPartyIdsAttribute) |

**Total new tests: 35** (367 → 402)

### Approach

- Used **testable subclass pattern** (`TestableContextHandler`) to expose 13 `protected` methods
  without modifying production code or using `InternalsVisibleTo`.
- `ContextHandler` has 12 constructor dependencies — all mocked except `IMemoryCache` (real `MemoryCache`).
- `GetResourceAttributeValues` (lines 286–391) was the highest-value target: 13 attribute-type branches,
  16 tests covering every branch plus edge cases.
- `EnrichResourceParty` tested with 5 scenarios covering org-number, person-id, and party-uuid lookup paths.
- Cached lookup methods (`GetRoles`, `GetMainUnits`, etc.) tested to verify correct service delegation.
- Complex orchestration methods (`Enrich`, `EnrichSubjectAttributes`, `EnrichResourceAttributes`)
  already covered by 6 existing integration tests — unit testing deferred.

### Verification

- [x] Build passes (0 errors)
- [x] All 402 Authorization.Tests pass (367 existing + 35 new)

---

## Final Coverage Results (measured)

| Assembly | Line% Before | Line% After | Branch% Before | Branch% After | Threshold |
|---|---|---|---|---|---|
| **Altinn.Authorization** | 62.77 | **70.91** | 63.50 | **70.93** | 60% ✅ |
| **Altinn.Authorization.ABAC** | 62.91 | **63.41** | 61.29 | **63.83** | 60% ✅ |
| **Altinn.Authorization.PEP** | 60.20 | **77.75** | 56.25 | **76.10** | 75% ✅ |

All CI thresholds exceeded. Total new tests: **184** Authorization + **52** PEP = **236 new tests**.

## Test Quality Assessment

| Category | Tests | % of new |
|---|---|---|
| **Real logic** (parsing, branching, mapping — zero or incidental mocks) | ~137 | ~74% |
| **Mock-wiring / thin delegation** (verify service called) | ~51 | ~26% |

Weakest tests (candidates for future pruning):
- Cached lookup forwarders in `ContextHandlerUnitTest` (6 tests) — trivial delegation
- `AccessListAuthorizationTest` (5 tests) — logic is just null-check + `.Any()`
- `PDPAppSITest` (4 tests) — mostly mock wiring, real work in `AuthorizationApiClient`

## Phase 6 Status: ✅ Complete (actionable items)

Phase 6 coverage maximization is **done** for all projects that don't require Docker or
external storage accounts. Remaining sub-steps are blocked on infrastructure:

| Sub-step | Status | Blocker |
|---|---|---|
| 6.1 Full baseline | ⬜ | Docker (AccessMgmt Testcontainers) |
| 6.2 Triage uncovered logic | ⬜ | Depends on 6.1 |
| 6.5 Host.Lease tests | ⬜ | Azure Storage account |
| 6.7g AccessManagement coverage | ⬜ | Docker |

---

**Note:** For current recommendations and next steps, see [INDEX.md](INDEX.md).
