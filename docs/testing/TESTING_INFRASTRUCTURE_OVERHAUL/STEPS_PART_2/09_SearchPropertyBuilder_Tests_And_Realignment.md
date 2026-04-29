---
step: 9
title: SearchPropertyBuilder unit tests + Part 2 plan realignment
phase: B
status: complete
linkedIssues:
  feature: null
  task: 2988
bugClassesCovered:
  - "Wrong property-name extraction for nested member expressions (e.g. pkg.Area.Group.Name → Area_Group_Name) — silently misweights PackageService.Search"
  - "Add overwrites the previous entry for the same expression — pinning replace-not-accumulate semantics"
  - "AddCollection produces wrong join string for combined (',') vs detailed (' | ') mode — would lose hits on resource-collection fuzzy search"
  - "GetPropertyName falls through to UnknownProperty for value-type members wrapped in a UnaryExpression (boxing)"
  - "Method-call expression branch returns the method name as the dictionary key (pkg => pkg.GetSomething() → 'GetSomething')"
  - "Fluent Add / AddCollection no longer return the same builder instance — would break PackageService's chained registration"
  - "Build() defensive-copies the internal dictionary — caller-mutability boundary documented"
verifiedTests: 19
touchedFiles: 4
---

# Step 9 — `SearchPropertyBuilder` unit tests + Part 2 plan realignment

## Goal

Add unit tests for the first real-logic class identified after the
audit's per-assembly coverage-% framing was superseded at Step 7
(see [Decision Log entry 2026-04-29](../TESTING_INFRASTRUCTURE_OVERHAUL_PART_2.md#decision-log)).
Bundle the long-overdue plan-realignment edits to `PART_2.md` and
this `INDEX.md` into the same PR, per the rule that tracking-doc
edits ride along with code rather than shipping as standalone PRs.

The test target — [`Altinn.AccessMgmt.Persistence.Core.Utilities.Search.SearchPropertyBuilder<T>`](../../../src/apps/Altinn.AccessManagement/src/Altinn.AccessMgmt.Persistence.Core/Utilities/Search/SearchPropertyBuilder.cs)
— is the fluent builder used directly by `PackageService.Search` to
register weighted properties for fuzzy matching. It has expression-tree
introspection with non-trivial branching, no DB or external
dependencies — a real-logic target under the *"real logic vs
pass-through wiring"* filter.

## What changed

### Code

`src/apps/Altinn.AccessManagement/test/Altinn.AccessMgmt.Core.Tests/PersistenceCore/SearchPropertyBuilderTest.cs`

19 new tests pinning down the bug classes listed in the front matter.
Test sections:

| Section | Tests | Purpose |
|---|---|---|
| `Add` — happy path & key derivation | 8 | Single property; nested members (2-level + 3-level); unary-expression boxing for value-type members; method-call expressions; replace-not-accumulate; distinct keys for distinct expressions; the compiled selector evaluates against an actual instance + through nested chains |
| `AddCollection` — combined vs detailed | 5 | Combined-mode key (`Resources (Combined)`); detailed-mode key (`Resources (Detailed)`); combined-mode joins with `, `; detailed-mode joins with ` | `; empty collection produces empty string; both modes coexist on the same property under distinct keys |
| Fluent contract | 2 | `Add` and `AddCollection` both return the same builder instance |
| `Build()` — empty + structural | 2 | Empty builder returns empty dict; `Build()` returns the live internal dictionary (mutability boundary documented) |

No changes to production code — `SearchPropertyBuilder<T>` was already
correct on the paths these tests exercise. The tests are the
regression net.

### Tracking-doc realignment

`docs/testing/TESTING_INFRASTRUCTURE_OVERHAUL/TESTING_INFRASTRUCTURE_OVERHAUL_PART_2.md`

- **Top-of-doc banner** added immediately under the title stating the
  two standing testing principles (mock tests must exercise real
  logic; coverage % is informational, not a planning lens) and
  enumerating the practical effect on Phases A through F.
- **Phase B section banner** marks the entire phase as needing
  per-candidate re-scope under a *"real logic vs pass-through wiring"*
  filter.
- **Phase F section banner** marks the phase as deprioritized; notes
  that any small Phase F Task that does open should reframe existing
  floors as *catastrophic-regression tripwires*, not as quality gates;
  cross-references the SonarCloud-side eventual follow-up.
- **Decision Log entry** dated *2026-04-29 (Part 2 Step 7)* records
  the realignment decisions: B.1 closed in original Moq form,
  addressed in integration-test form (Step 7); Phase B per-candidate
  filter; Phase F deprioritized; step-doc front matter adds
  `bugClassesCovered` as the lead field; tracking-doc edits bundle
  with code PRs rather than shipping as standalone PRs.

`docs/testing/TESTING_INFRASTRUCTURE_OVERHAUL/STEPS_PART_2/INDEX.md`

- **Top-of-doc banner** points readers to PART_2.md's banner +
  Decision Log.
- **Step doc template** revised: `bugClassesCovered:` is now the
  **lead measurement field**, ahead of `verifiedTests` and
  `coverageDelta`. `coverageDelta` is explicitly marked
  *"informational only — not a goal"*. Adds a paragraph explaining
  that step docs unable to name at least one bug class probably
  violate the *"mock tests must exercise real logic"* convention.
- **Step log rows 7, 8, 9** added for: PR [apps#2984](https://github.com/Altinn/altinn-authorization-tmp/pull/2984)
  (Metadata integration tests + 4 PackageService bug fixes — the
  effective replacement for the closed B.1 attempt at PR
  [apps#2978](https://github.com/Altinn/altinn-authorization-tmp/pull/2978)),
  PR [apps#2987](https://github.com/Altinn/altinn-authorization-tmp/pull/2987)
  (CI Report-failed-tests fix), and this Step 9.
- **Recommended Next Steps** updated: Phase B header changed to
  *"re-scoped at Step 7"*; B.1 struck through and pointed at Step 7;
  B.2 marked *partially in progress* (continues with the rest of
  `Persistence.Core` + `AccessMgmt.Core`); B.3 annotated with the
  per-candidate filter; ~~F.1~~ and ~~F.2~~ marked *deprioritized
  2026-04-29*.
- **Final Coverage table preamble** explicitly marks the table as
  *"informational snapshot — not a target list"*, with a note that
  assemblies below 60 % are not automatically next steps and
  assemblies above 70 % are not automatically Phase F candidates.
- **Blocked Items** `Last re-checked` column refreshed to step 9 for
  all three rows. None unblocked.

## Verification

### Tests

```
SearchPropertyBuilderTest (focused):  19 / 19 passed (0 failed, 0 skipped) — 3.6s
Altinn.AccessMgmt.Core.Tests (full): all passing (no sibling regressions)
```

### Coverage

`Altinn.AccessMgmt.Persistence.Core` baseline (Step 6): 25.39 % line.
The 19 new tests cover `SearchPropertyBuilder<T>` (~127 LOC) end-to-end
including the expression-tree branches that were previously
unreachable from the test surface. Per the realigned step-doc
template, no specific delta target — the gain is informational, not
the reason to ship. Re-measure during a future closing sweep if the
team wants the number.

### `check-coverage-thresholds.ps1`

Not run for this step (Persistence.Core has no enforced floor; would
not change exit). Threshold check exit on the most recent full-suite
run remains 0.

## Deferred / follow-up

- **Rest of `Persistence.Core`** — the audit at the top of Step 9
  identified `MssqlQueryBuilder` (sibling to the already-tested
  `PostgresQueryBuilder`) as a possible next target, but the codebase
  is Postgres-only and the MSSQL builder may be vestigial. Worth a
  separate audit before testing it would earn its keep.
- **`AccessMgmt.Core` (the other half of B.2)** — separate Task,
  same per-candidate filter applies. Not picked up here so this PR
  stays scoped.
- **Existing closed PRs preserved as historical context** — the
  closed-without-merging Task #2977 / PR #2978 (B.1 Moq attempt) and
  Feature #2979 / Task #2980 / PR #2982 (docs-only realignment) sit
  in the issue tracker as the historical record of the realignment
  pivot. The Decision Log entry records the resulting decisions; the
  PR threads themselves are the canonical context for anyone
  curious.
- **SonarCloud-side exclusions for pass-through code** — eventual
  follow-up; Sonar's coverage gate should also exclude pass-through
  code so the same pattern doesn't re-emerge via Sonar pressure.
  Not in scope for this step.
- **Phase F reframing as catastrophic-regression tripwires** — only
  if/when the team decides such a Task is worth opening; not on the
  Recommended Next Steps list pending that decision.
