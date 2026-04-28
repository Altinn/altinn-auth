---
step: 6
title: T1 closing sweep — re-baseline against post-C5'-fix merged cobertura
phase: A
status: complete
linkedIssues:
  feature: 2946
  task: 2947
coverageDelta:
  # Step 6 doesn't change production code; it re-records the baseline
  # against the now-correct merged-cobertura view (which also reflects
  # Step 5's flag-removal dead-code shrink). The notable Step 1 ->
  # Step 6 deltas (the C5' fix surfacing previously-mis-counted
  # coverage) are documented below and in PART_2.md §1.4 / §1.6.
  notes: re-baseline-only; see step doc for Step 1 -> Step 6 deltas
verifiedTests: 2536
testFailures: 0
testSkips: 16
testProjectsWithZeroDiscovered: 0
touchedFiles: 3
auditIds: []
---

# Step 6 — T1 closing sweep

## Goal

Close out Task T1 (#2947). After Steps 2–5 resolved the four bundled
audit findings (C1', C2', C3', C5'), re-baseline the audit numbers
against the canonical merged-cobertura view, re-prioritise Phase B
based on the now-revealed true coverage, and update the Final
Coverage table + Recommended Next Steps so the next step picks up a
self-consistent state. After this commit T1's bundle is ready for a
single bundled PR (per user direction; α and β are bundled together).

## What changed

### Coverage measurement (re-run to capture post-Step-5 state)

Ran `eng/testing/run-coverage.ps1 -NoBuild` end-to-end with all the
fixes from Steps 2–5 in place. Result (from the threshold check tail
of the run):

```
Warning - below ratchet (non-fatal):
  Altinn.AccessManagement (57.57% < 60%)

All enforced assemblies meet their coverage thresholds.
```

Test totals: **2536 / 2520 / 0 / 16** (Total / Pass / Fail / Skip).

| | Pre-T1 (Step 1) | Post-T1 (Step 6) | Δ |
|---|---:|---:|---|
| Test projects | 11 (one empty) | 11 (different roster) | net 0 |
| Total tests | 2535 | 2536 | +1 (Step 4 smoke test) |
| Failing tests | **1** | 0 | -1 (Step 5 flag removal) |
| Discovered=0 projects | 1 | 0 | -1 (Step 3 deletion) |

### `docs/testing/TESTING_INFRASTRUCTURE_OVERHAUL/TESTING_INFRASTRUCTURE_OVERHAUL_PART_2.md`

- **Status banner** updated: 🟢 Phase A complete; T1 #2947 ships
  Steps 2–5; Phases B–F still pending.
- **§1.1 Test project inventory** — refreshed to current state (11
  projects, ABAC.Tests removed in Step 3, Pipeline.Tests added in
  Step 4, Authorization.Tests now 402/402 after Step 5). Anomaly
  callouts removed; project-roster delta noted.
- **§1.4 Coverage baseline** — replaced Step 1's max-across-files
  table with the Step 6 merged-cobertura table. Added a new column
  "Δ vs Step 1 audit" so readers can see at a glance which numbers
  shifted (and by how much) purely from the C5' measurement fix or
  Step 5's dead-code removal.
- **§1.5 Sub-60% classification** — refreshed every number; pulled
  `Api.Internal` out of the pure-logic-reachable list (now 73.63% —
  passing implicit threshold); softened M3' urgency for
  `AccessManagement.Persistence` (57.29% vs the Step 1 figure of
  44.90%).
- **§1.6 Drift summary** — now distinguishes "real regressions"
  (`ServiceOwner -2.16pp`, `Authorization -1.77pp`, `AccessManagement
  main -0.62pp`) from "measurement artefacts" (5 assemblies
  under-counted by ≥ 9pp purely from the C5' bug).
- **§2 medium findings** — M2' / M3' / M5' / M6' / M7' all updated
  with new numbers; M6' explicitly re-scoped to drop `Api.Internal`
  ("only `Api.Metadata` remains as a controller gap").
- **§2 low findings** — L2' (threshold ratchet) candidate list
  refreshed; `Api.Internal` added as NEW Phase F promotion candidate
  (floor 70). L3' note added that the warn-floor delta is 2.43pp not
  3.53pp.
- **§4 Phase B** — B.1 explicitly drops `Api.Internal` with a Step 6
  callout; B.2 reorders to put `Persistence.Core` first (largest
  remaining pure-logic gap); B.3 updates Integration.Platform's
  number.
- **Decision Log** — three new rows: (1) Step 5 rectification
  (replacing the original cross-user-check hoist with full flag
  removal after the architect's confirmation); (2) decision to
  re-baseline §1 against merged cobertura at Step 6; (3) decision to
  bundle PR α + PR β into a single PR per user direction.
- **Last-updated stamp** — refreshed.

### `docs/testing/TESTING_INFRASTRUCTURE_OVERHAUL/STEPS_PART_2/INDEX.md`

- **Step log** — added Step 6 row.
- **Final Coverage** — replaced the Step 1 max-across-files table
  with the Step 6 merged-cobertura version. Status column now flags
  Phase F promotion candidates explicitly.
- **Recommended Next Steps** — Phase B (items 6–8) re-scoped: B.1
  drops `Api.Internal`; B.2 puts `Persistence.Core` first; B.3 uses
  Step 6 number. Phase F (items 19–20) updated: 5 promotion
  candidates listed with current numbers; floor-raise candidates
  named (Maskinporten 75→80, PEP 75→78, PersistenceEF 90→95);
  L3' note refreshed.
- **Blocked Items** — `Last re-checked = step 6` on all 3 rows.

## Verification

The Step 6 sweep is documentation-only (no production code or
tooling change beyond what Steps 2–5 already shipped). Verification
of correctness is therefore: do the docs match the measured state?

- **Coverage table values** — copied verbatim from the threshold
  check output above. Spot-checked: PersistenceEF 99.03% / 95.53%,
  Pipeline 0.24% / 0.00%, AccessManagement 57.57% / 60.64%,
  Authorization 69.14% / 72.72% — match the run output.
- **Test counts** — copied from per-project `*.coverage.log` files.
- **Step deltas** — Step 1 → Step 6 deltas computed by hand against
  the Step 1 audit table (preserved in Step 1 doc's `coverageDelta:`
  front-matter). The positive-delta side of the ledger is dominated
  by the C5' fix surfacing previously-mis-counted coverage; Step 5's
  flag removal contributes a small further uptick on
  `Altinn.Authorization` (69.09% → 69.14%) by removing dead-code
  lines from the denominator.
- **No regressions** — coverage threshold check exit 0; only the
  legitimate `Altinn.AccessManagement (57.57% < 60%)` warn-only line.
  Confirms Steps 2–5 didn't accidentally drop any other assembly
  below an enforced floor.

## Deferred / follow-up

- **Open the bundled PR** for T1 (#2947). Per user direction this is
  one PR (α∪β) covering all 6 commits on
  `feat/2947_test_infrastructure_critical_fixes`, with an explicit
  "`AccessManagementAuthorizedParties` flag removal included; see
  `PartiesController.cs` diff" callout in the body so the production
  diff is easy to find amidst the broader doc/tooling changes.
- **Future Phase B kickoff** picks up the now-self-consistent
  Recommended Next Steps. Step 7 candidates (priority order):
  - B.1 — `Api.Metadata` controller gap (smallest remaining
    pure-logic target).
  - B.2 — `Persistence.Core` (largest pure-logic gap at 25.39%).
  - F.1 — Phase F promotion (zero coverage risk now that the script
    is fixed; mostly a JSON edit + CI-run verification).
- **M2'** investigation (`AccessManagement` main-app -0.62pp
  regression) needs a Phase E.2 step at some point; not blocking
  Phase B.

## Blocked-items sweep

No items unblocked. Refresh `Last re-checked = step 6` on the three
Blocked Items rows in [`INDEX.md`](INDEX.md#blocked-items).

## Obsolete-docs sweep

None. The Step 1 max-across-files numbers in PART_2.md §1.4 are
superseded by this step's re-baseline, but they're preserved in the
Step 1 doc's `coverageDelta:` front-matter — no doc deletion needed.
