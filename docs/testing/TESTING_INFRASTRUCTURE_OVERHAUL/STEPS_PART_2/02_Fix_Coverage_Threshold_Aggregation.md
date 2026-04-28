---
step: 2
title: Fix coverage-threshold aggregation in workstation runs
phase: A
status: complete
linkedIssues:
  feature: 2946
  task: 2947
coverageDelta:
  # No production code changed — this step fixes the local-dev tooling.
  # However, the corrected aggregation reveals that the Step 1 audit
  # under-counted several assemblies' coverage. See Verification §
  # "Audit baseline correction" below for the deltas.
  notes: tooling-only; baseline numbers in audit doc need a delta-update follow-up
verifiedTests: 0
touchedFiles: 2
auditIds: [C5']
---

# Step 2 — Fix coverage-threshold aggregation in workstation runs

## Goal

Resolve audit finding **C5'**: `eng/testing/check-coverage-thresholds.ps1`
reported 22+ false-positive `Below threshold:` failures when given
multiple cobertura inputs (the workstation flow). Make the local
`run-coverage.ps1` produce a single canonical aggregate before checking
thresholds, and harden `check-coverage-thresholds.ps1` to handle
multi-file inputs correctly as defense-in-depth. CI is unaffected
(it already passes a single merged cobertura).

## What changed

### `eng/testing/run-coverage.ps1`

After the parallel `dotnet-coverage collect` loop produces the 11
per-project cobertura files, added a `dotnet-coverage merge` step that
aggregates them into `TestResults/coverage.cobertura.xml`. This single
merged file is then passed to:

- `check-coverage-thresholds.ps1` (one-element `CoverageFiles` array)
- `reportgenerator` (single `-reports:` argument)

Per-project files remain in `TestResults/` for ad-hoc debugging; the
merged file is the canonical source of truth.

If the merge fails, the script exits 1 with a clear message — preserves
the existing fail-fast behaviour.

### `eng/testing/check-coverage-thresholds.ps1`

Restructured the package-iteration loop into two phases:

- **Phase 1 — aggregate.** Walk every `<package>` in every input
  cobertura file. Build a hashtable keyed by assembly name, holding
  the **max** `line-rate`, **max** `branch-rate`, and **union** of the
  `Owned` verdict across all occurrences.
- **Phase 2 — print + threshold-check.** Iterate the hashtable in
  sorted-by-name order, printing each assembly exactly once and
  applying the threshold check exactly once.

When `run-coverage.ps1` calls this script with the merged
`coverage.cobertura.xml`, Phase 1 sees each assembly exactly once and
the max-aggregation is a no-op. When the script is invoked directly
with multiple cobertura files (e.g. ad-hoc debugging or any future
caller that hasn't merged), the max-aggregation is a sound
conservative upper-bound that prevents the false-positive cascade —
worst case it under-reports vs the precise union, but it never
over-reports above the true coverage and never produces spurious
failures. Comments in the file document this trade-off.

## Verification

### Pre-commit component checks (without re-running tests)

Reused the 11 cobertura files left in `TestResults/` from the Step 1
baseline run. All checks below ran in seconds.

**Check 1 — defensive aggregation in the threshold script.** Invoked
`check-coverage-thresholds.ps1` directly with the 11 per-project
cobertura files:

```pwsh
$files = Get-ChildItem TestResults -Filter '*.cobertura.xml' | % FullName
& eng\testing\check-coverage-thresholds.ps1 -CoverageFiles $files -OwnedRoot .
```

Result: each assembly appears exactly once in the summary; the
previously-spurious `Below threshold:` cascade (22+ entries) is gone;
the only remaining warn-only line is the legitimate
`Altinn.AccessManagement (56.47% < 60%)` ratchet (audit M2'/L3'). Exit
code 0.

**Check 2 — `dotnet-coverage merge` produces a valid cobertura.**

```pwsh
dotnet-coverage merge --output TestResults\coverage.cobertura.xml `
    --output-format cobertura TestResults\*.cobertura.xml
```

Result: exit 0; 42 MB output; XML parses; **54 distinct `<package>`
elements** (each unique assembly appears exactly once, vs ~90+
duplicate-laden entries when summed across the 11 inputs).

**Check 3 — the check script against the single merged file.** Same
output shape as Check 1 but with **higher numbers** for several
assemblies (see "Audit baseline correction" below).

### Audit baseline correction (positive ripple effect)

Properly-merged cobertura aggregates line/branch coverage as a *union*
of covered lines, whereas Step 1's max-across-files heuristic is a
conservative lower-bound. Several assemblies are materially higher
than the audit recorded:

| Assembly | Step 1 baseline (max) | Merged (true union) | Δ |
|---|---:|---:|---:|
| `Altinn.AccessManagement.Api.Internal` | 48.56% | **73.63%** | +25.07pp 🎯 |
| `Altinn.AccessManagement.Persistence` | 44.90% | **57.29%** | +12.39pp |
| `Altinn.Authorization.Integration.Platform` | 45.38% | **54.94%** | +9.56pp |
| `Altinn.AccessMgmt.Core` | 33.66% | **44.96%** | +11.30pp |
| `Altinn.Authorization.Api.Contracts` | 23.57% | **34.68%** | +11.11pp |
| `Altinn.AccessManagement.Core` | 63.29% | **66.49%** | +3.20pp |
| `Altinn.AccessManagement` (main app) | 56.47% | **57.57%** | +1.10pp |
| `Altinn.AccessMgmt.PersistenceEF` | 98.78% | **99.03%** | +0.25pp |
| `Altinn.Authorization.PEP` | 78.99% | **79.60%** | +0.61pp |

Implications for the audit findings:

- **M6'** (`Api.Internal` controller gap, listed at 48.56%) is largely
  resolved — actual coverage is 73.63%. `Api.Metadata` (51.53% in both
  views) remains the controller gap.
- **M3'** (live-DB Npgsql repos): `AccessManagement.Persistence`
  (44.90% → 57.29%) is closer to threshold than the audit recorded.
  `AccessMgmt.Persistence` is unchanged at 47.32% in this snapshot
  (the run did not pick up improved coverage between the two views).
- **M2'/L3'** (`AccessManagement` main-app regression): still below 60%
  even under proper aggregation (57.57%), so the regression-vs-Step 12
  finding stands but is less severe than the 56.47% number suggested.

Per the [INDEX.md sweep rule](INDEX.md#when-completing-a-step), the
[`TESTING_INFRASTRUCTURE_OVERHAUL_PART_2.md`](../TESTING_INFRASTRUCTURE_OVERHAUL_PART_2.md)
§1.4 baseline table and the [Final Coverage table](INDEX.md#final-coverage-measured)
in INDEX.md should be re-baselined against the merged-cobertura view.
Doing that is **deferred to the end of this Task** (T1) so all the
other commits land first and the re-baseline reflects the post-Phase-A
state in one update, not five. See the deferred-items section below.

### End-to-end orchestration check

Ran `pwsh -NoProfile -ExecutionPolicy Bypass -File eng/testing/run-coverage.ps1 -NoBuild`
end-to-end after the component checks. Result:

- `dotnet-coverage merge` produced `TestResults/coverage.cobertura.xml`
  (42 MB) from the 11 per-project files.
- The threshold check ran against the single merged file and reported
  each assembly exactly once.
- Only the legitimate warn-only line printed:
  `Altinn.AccessManagement (57.57% < 60%)` — the same M2'/L3'
  regression flagged by Step 1.
- Final line: `All enforced assemblies meet their coverage thresholds.`
- The script exited 1 due to the **pre-existing C2'** test failure
  (`Altinn.Authorization.Tests` — `ValidateParty_NotAsAuthenticatedUser_Forbidden`,
  the auth regression / mock drift queued for a later step under this
  same Task). Pre-Step-2 the exit 1 had two causes (test failure **and**
  22+ false-positive `Below threshold:` failures); post-Step-2 only the
  one legitimate cause remains. Confirms C5' is fully resolved.

## Deferred / follow-up

- **Audit-doc baseline refresh** — re-baseline §1.4 of
  `TESTING_INFRASTRUCTURE_OVERHAUL_PART_2.md` and the Final Coverage
  table in `INDEX.md` against the merged-cobertura view once T1's
  remaining sub-items (Steps 3–5) have landed. Tracked under T1's
  closing checklist; a single sweep-step at the end is cleaner than
  five mid-Task amendments.
- **Decision needed:** several Phase B items (e.g. M6' Api.Internal)
  have effectively closed without any code change — they were
  already-passing, just mis-measured. The Phase B Recommended Next
  Steps in INDEX.md (item 6) should be updated when the audit
  baseline is refreshed; consider whether to re-prioritise other
  Phase B work given the freed-up capacity.

## Blocked-items sweep

No items unblocked. Refresh `Last re-checked = step 2` on the three
Blocked Items rows.

## Obsolete-docs sweep

None. No prior step doc covered this work.
