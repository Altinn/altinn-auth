# Standardize Test Patterns & Naming

## Goal

Establish consistent, readable test conventions across all test projects.

## Changes

### 4.1 Test Naming Convention (documented)

Created `docs/testing/TEST_NAMING_CONVENTION.md` with the adopted standard:
`MethodUnderTest_Scenario_ExpectedResult`.

The existing codebase has three dominant patterns:
- **Descriptive:** `TryDeleteDelegationPolicyRules_Valid` — good, follows convention
- **Opaque numbered:** `PDP_Decision_AltinnApps0001`, `HandleRequirementAsync_TC01Async` — hard to understand without reading the test body
- **Mixed:** `WritePolicy_TC03`, `AccessList_Authorization_Permit_WithoutActionFilter`

The convention is documented for new tests. Renaming existing tests is deferred to
avoid noisy diffs and merge conflicts on an active codebase.

### 4.5 GlobalSuppressions.cs → .editorconfig

Both `Authorization.Tests` and `PEP.Tests` had `GlobalSuppressions.cs` files
suppressing `SA1600` (elements should be documented) for the entire assembly.

- Moved the suppression to `.editorconfig` using a `[**/test/**/*.cs]` section
- Deleted both `GlobalSuppressions.cs` files

### 4.6 Dead Code Cleanup in csproj Files

| Project | Removed | Reason |
|---|---|---|
| `Altinn.Authorization.Tests.csproj` | `<Folder Include="Webfactory\" />` | Empty folder — `CustomWebApplicationFactory` deleted in Phase 2 |
| `AccessMgmt.Tests.csproj` | `<Folder Include="Data\ResourceRegistryResources\nav_sykepenger_dialog\" />` | Directory has content; `Folder Include` only tracks empty dirs |

The `<None Remove="Data\AuthorizedParties\TestDataAppsInstanceDelegation.cs" />`
entry in `AccessMgmt.Tests.csproj` was kept — it prevents the `.cs` file (which
lives under `Data\**` None-glob) from being double-included as both None and Compile.

### Items Deferred

| Item | Reason |
|---|---|
| 4.2 FluentAssertions adoption | Already present in `.editorconfig` (`FAA0002`), evaluate in Phase 6 |
| 4.3 AcceptanceCriteriaComposer review | Scoped to `AccessMgmt.Tests`; deferred with Phase 2.2-2.3 |
| 4.4 `[Collection]` standardization | Requires per-test-class audit; low risk, defer |

## Verification

- [x] Build passes
- [x] All tests still pass (210/210 Authorization.Tests)
- [x] SA1600 no longer fires for test project files

---

## Next Phase: Phase 6 — Maximize Code Coverage

> All prerequisites are met: Phases 1–5 are complete.
> Phase 2.2–2.3 (AccessMgmt.Tests WAF consolidation) and Phase 3.2–3.4
> (mock dedup implementation) remain deferred but do **not** block coverage work.

### Baseline (from Phase 5)

| Assembly | Line% | Branch% |
|---|---|---|
| Altinn.Authorization | 62.77 | 63.50 |
| Altinn.Authorization.ABAC | 62.91 | 61.29 |
| Altinn.Authorization.PEP | 7.68 | 13.60 |

### Execution Plan

Create `docs/testing/steps/Step_6_Maximize_Coverage.md` with:

| Sub-step | Task | Priority |
|---|---|---|
| **6.1** | Run `run-coverage.ps1` across **all 11 test projects** to establish a full baseline (not just Authorization). | 🔴 High |
| **6.2** | Triage uncovered code: identify critical business-logic paths in `Altinn.Authorization`, `Altinn.Authorization.ABAC`, and `Altinn.AccessMgmt.Core` that lack any test coverage. | 🔴 High |
| **6.3** | **PEP coverage sprint** — `Altinn.Authorization.PEP` is at 7.68% line coverage. Add unit tests for `AppAccessHandler`, `DecisionHelper`, and the PDP client. | 🔴 High |
| **6.4** | Add edge-case / error-path tests for `Authorization.Tests` (target: 75%+ line). | 🟡 Medium |
| **6.5** | Add tests for `Altinn.Authorization.Host.Lease` and other Host libraries. | 🟡 Medium |
| **6.6** | Set a CI threshold (e.g., `-Threshold 50` initially, ratchet up as coverage improves). | 🟡 Medium |
| **6.7** | Improve coverage for AccessManagement API projects (Enduser, ServiceOwner, Api). | 🟢 Lower |

### Approach

1. **Measure first** — run full coverage baseline before writing any new tests.
2. **Focus on high-value gaps** — prioritize business logic and security paths
   over boilerplate/DTOs.
3. **Ratchet threshold** — start with a threshold that passes today, then bump it
   with each PR that adds coverage.
4. **Don't chase 100%** — target 70–80% line coverage for core libraries;
   diminishing returns beyond that.
