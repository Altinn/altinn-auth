---
step: 1
title: Part 2 kickoff — fresh infrastructure audit
phase: KICKOFF
status: complete
coverageDelta:
  # Step 1 measured the baseline; it didn't move coverage. Each entry below is
  # the absolute Step 1 line% (per the §1.4 max-across-files rule), not a delta.
  Altinn.AccessMgmt.PersistenceEF:           { line: 98.78, branch: 91.75 }
  Altinn.Authorization.Host:                 { line: 91.67, branch: 50.00, note: newly_measured }
  Altinn.AccessManagement.Integration:       { line: 88.35, branch: 87.50 }
  Altinn.AccessManagement.Api.Maskinporten:  { line: 80.36, branch: 80.00 }
  Altinn.Authorization.PEP:                  { line: 78.99, branch: 78.68 }
  Altinn.AccessManagement.Api.Enduser:       { line: 73.88, branch: 65.32 }
  Altinn.AccessManagement.Api.ServiceOwner:  { line: 69.58, branch: 57.94 }
  Altinn.Authorization:                      { line: 69.00, branch: 72.61 }
  Altinn.AccessManagement.Api.Enterprise:    { line: 66.39, branch: 56.52 }
  Altinn.AccessManagement.Core:              { line: 63.29, branch: 60.38 }
  Altinn.Authorization.ABAC:                 { line: 63.17, branch: 61.29, note: indirect }
  Altinn.Authorization.Host.Database:        { line: 59.42, branch: 65.62, note: newly_measured }
  Altinn.AccessManagement:                   { line: 56.47, branch: 59.35, note: regression_-1.72pp }
  Altinn.AccessManagement.Api.Metadata:      { line: 51.53, branch: 51.11 }
  Altinn.AccessManagement.Api.Internal:      { line: 48.56, branch: 49.46 }
  Altinn.AccessMgmt.Persistence:             { line: 47.32, branch: 33.40 }
  Altinn.Authorization.Integration.Platform: { line: 45.38, branch: 64.12 }
  Altinn.AccessManagement.Persistence:       { line: 44.90, branch: 29.05 }
  Altinn.AccessMgmt.Core:                    { line: 33.66, branch: 25.37 }
  Altinn.AccessMgmt.Persistence.Core:        { line: 25.34, branch: 24.30 }
  Altinn.Authorization.Api.Contracts:        { line: 23.57, branch: 12.58 }
  Altinn.Authorization.Host.Lease:           { line:  6.87, branch:  7.41, note: azurite_blocked }
  Altinn.Authorization.Host.Pipeline:        { line:  0.00, branch:  0.00, note: no_test_project }
verifiedTests: 2535
testFailures: 1                # ValidateParty_NotAsAuthenticatedUser_Forbidden — see C2'
testSkips: 16                  # 9 Enduser + 5 Integration + 2 Host.Lease — see L1'
testProjectsWithZeroDiscovered: 1   # Altinn.Authorization.ABAC.Tests — see C1'
touchedFiles: 3
issuesIdentified: { critical: 5, medium: 8, low: 5 }
---

# Step 1 — Part 2 kickoff: fresh infrastructure audit

## Goal

Populate sections 1–4 of [`../TESTING_INFRASTRUCTURE_OVERHAUL_PART_2.md`](../TESTING_INFRASTRUCTURE_OVERHAUL_PART_2.md)
with a fresh measurement-based audit, produce the new issue list under
the `C1'`/`M1'`/`L1'` namespace, and seed
[`INDEX.md` § Recommended Next Steps](INDEX.md#recommended-next-steps-priority-order)
from the resulting six-phase plan.

## What changed

### `docs/testing/TESTING_INFRASTRUCTURE_OVERHAUL/TESTING_INFRASTRUCTURE_OVERHAUL_PART_2.md`

Sections 1–4 + 5 + a Decision Log row populated:

- **§1 Current State Audit** — test-project table (11 projects, 2535
  tests, ~6.5 min wall-clock), fixture table (5 fixtures + `PostgresServer`
  singleton with file paths and consumer counts), mock-duplication
  inventory, full coverage baseline (23 owned assemblies; max-across-files
  aggregation rule documented), sub-60% classification by closing
  approach, drift summary.
- **§2 Findings & Issues** — fresh `C1'`–`C5'` (5 critical), `M1'`–`M8'`
  (8 medium), `L1'`–`L5'` (5 low) under a clean ID namespace.
- **§3 Best Practices Already Followed** — 11 carry-forward items
  including the centralized MTP wiring in
  [`src/Directory.Build.targets:8-15`](../../../../src/Directory.Build.targets:8)
  and the auto `InternalsVisibleTo` at line 68.
- **§4 Improvement Plan — Phases** — six phases (A critical → B
  pure-logic → C live-DB → D new projects → E housekeeping → F ratchet),
  each broken into numbered sub-items mapped back to issue IDs.
- **§5 Execution Order & Dependencies** — explicit prerequisite graph
  (A.3 → F, A.5 → D.1, D.2 → L1'.Lease, C.1 → C.2).
- **Decision Log** — added two new rows: max-across-files aggregation
  rule, and Phase A → Phase F sequencing rationale.

### `docs/testing/TESTING_INFRASTRUCTURE_OVERHAUL/STEPS_PART_2/INDEX.md`

- Step log: added Step 1 row (this doc).
- Recommended Next Steps: replaced the kickoff item with a 21-item
  ordered list mapped to the six phases.
- Blocked Items: refreshed both Part 1 carry-overs to `Last re-checked = step 1`;
  added a third row for `Receiver_ApprovesPendingPackageRequest_ReturnsApproved`
  (Part 1 Step 62 carry-over, missed by the scaffold).
- Final Coverage table: populated all 23 owned assemblies with
  Step 1 numbers + threshold status + cross-refs to Phase IDs.

### `docs/testing/TESTING_INFRASTRUCTURE_OVERHAUL/STEPS_PART_2/01_Part_2_Kickoff_Audit.md`

This file. New.

## Verification

### Coverage measurement

Ran `eng/testing/run-coverage.ps1` (no args; default = all 11
`*.Tests.csproj` projects under `src/`) on Windows with Podman 5.2.5
(WSL machine `podman-machine-default` running). Single invocation; ~6.5
min wall-clock; 11 cobertura files emitted to `TestResults/`.

```
Total tests:    2535  (1196 + 351 + 323 + 92 + 58 + 45 + 41 + 25 + 2 + 0 + 402)
Passed:         2518
Failed:         1     (ValidateParty_NotAsAuthenticatedUser_Forbidden — C2')
Skipped:        16    (9 Enduser + 5 Integration + 2 Host.Lease — L1')
Discovered=0:   1     (Altinn.Authorization.ABAC.Tests — C1')
```

The threshold check at the end of the run reported 22 spurious
`Below threshold:` failures because the script processes each per-test-project
cobertura file independently — see issue **C5'**. The aggregated max-across-files
numbers in [`PART_2.md` §1.4](../TESTING_INFRASTRUCTURE_OVERHAUL_PART_2.md#14-coverage-baseline-line--branch-owned-assemblies-max-across-files)
were derived by parsing the 11 cobertura files manually with PowerShell.

### Per-project test counts (from `TestResults/*.coverage.log`)

| Project | Total | Pass | Fail | Skip | Time |
|---|---:|---:|---:|---:|---:|
| AccessMgmt.Tests | 1196 | 1196 | 0 | 0 | 130.97s |
| Altinn.AccessManagement.Api.Tests | 58 | 58 | 0 | 0 | 6.27s |
| Altinn.AccessManagement.Enduser.Api.Tests | 351 | 342 | 0 | 9 | 91.53s |
| Altinn.AccessManagement.ServiceOwner.Api.Tests | 25 | 25 | 0 | 0 | 54.93s |
| Altinn.AccessMgmt.Core.Tests | 323 | 323 | 0 | 0 | 41.0s |
| Altinn.AccessMgmt.PersistenceEF.Tests | 41 | 41 | 0 | 0 | 4.77s |
| Altinn.Authorization.ABAC.Tests | 0 | — | — | — | 0s |
| Altinn.Authorization.Host.Lease.Tests | 2 | 0 | 0 | 2 | 0.33s |
| Altinn.Authorization.Integration.Tests | 45 | 40 | 0 | 5 | 2.64s |
| Altinn.Authorization.PEP.Tests | 92 | 92 | 0 | 0 | 1.91s |
| Altinn.Authorization.Tests | 402 | 401 | 1 | 0 | 12.75s |

### Fixture & mock inventory

Verified by reading the 4 fixture source files directly
(`ApiFixture.cs`, `LegacyApiFixture.cs`, `PostgresFixture.cs`,
`AuthorizationApiFixture.cs`) plus the `PostgresServer` static singleton
in the same `PostgresFixture.cs` file. Mock inventory cross-checked via
`Glob` of `src/**/test/**/Mocks/**/*.cs` and `src/**/test/**/MockServices/**/*.cs`.

### Central MTP wiring

Verified
[`src/Directory.Build.targets:8`](../../../../src/Directory.Build.targets:8)
sets `OutputType=Exe` and
[`src/Directory.Build.targets:15`](../../../../src/Directory.Build.targets:15)
sets `TestingPlatformDotnetTestSupport=true` when
`IsTestProject=true && XUnitVersion=v3`. Confirmed every
`test/Directory.Build.props` (5 of them, one per vertical) sets
`<XUnitVersion>v3</XUnitVersion>` + `<IsTestProject>true</IsTestProject>`.
Confirmed every test csproj sets the empty-singular
`<TargetFramework></TargetFramework>` to clear the inherited singular
TFM (Part 1 Step 37 fix).

## Critical findings (more detail)

### C1' — `Altinn.Authorization.ABAC.Tests` is empty

`Glob src/pkgs/Altinn.Authorization.ABAC/test/**/*.cs` returns only
auto-generated files (`GlobalUsings.g.cs`, `XunitAutoGeneratedEntryPoint.cs`,
`AssemblyInfo.cs`). The test runner discovers 0 tests; cobertura output
is 181 bytes ("No code coverage data available. Profiler was not
initialized."). ABAC's 63.17% line coverage in §1.4 is **entirely
indirect** via `Altinn.Authorization.Tests` exercising PEP→ABAC paths.

### C2' — `ValidateParty_NotAsAuthenticatedUser_Forbidden` failing

[`PartiesControllerTest.cs:167`](../../../../src/apps/Altinn.Authorization/test/Altinn.Authorization.Tests/PartiesControllerTest.cs:167):

```
Assert.Equal() Failure: Values differ
  Expected: Forbidden
  Actual:   OK
```

Test scenario: authenticated user `20000490` calls
`GET authorization/api/v1/parties/50002598/validate?userid=1337` —
i.e. asks whether *another* user (1337) can act on party `50002598`.
The contract says: only the authenticated user can ask about *their own*
party-validation; cross-user requests must be 403. The test now sees
200, which is either:

- a real authorization regression in `PartiesController.ValidateParty(...)`
  (security finding), or
- `PartiesMock` / `RegisterServiceMock` returning a permissive answer
  it shouldn't.

Triage before any further coverage work in the Authorization vertical.

### C3' — `Altinn.Authorization.Host.Pipeline` has no test project

~1.4k LOC across `Builders/`, `Services/`, `HostedServices/`,
`Telemetry/`, `Extensions/` (largest single file:
`HostedServices/PipelineHostedService.cs` 345 lines). Zero coverage
because nothing in the test tree references it. Phase D.1 target.

### C4' — `AutoMapper 14.0.0` CVE

NU1903 / GHSA-rvv3-g6hj-g44x. Pinned in
[`src/Directory.Packages.props:27`](../../../../src/Directory.Packages.props:27).
Surfaced as a NU1903 warning in every `dotnet restore` of
`src/apps/Altinn.AccessManagement/src/Altinn.AccessManagement/Altinn.AccessManagement.csproj`
during the audit run.

### C5' — `check-coverage-thresholds.ps1` workstation false-positive

In workstation mode `run-coverage.ps1` feeds 11 cobertura files into
[`check-coverage-thresholds.ps1:110-134`](../../../../eng/testing/check-coverage-thresholds.ps1).
The `foreach ($pkg in $xml.coverage.packages.package)` loop applies the
threshold to **every** occurrence of an assembly across all 11 files.
Example from this audit:

```
Below threshold:
  Altinn.AccessManagement.Core (0.7% < 60%)        # transitive view from one test project
  Altinn.AccessManagement.Core (14.11% < 60%)      # ditto, different project
  Altinn.AccessManagement.Core (0.69% < 60%)       # ditto
  Altinn.AccessManagement.Core (1% < 60%)          # ditto
  ...                                              # canonical 63.29% never seen as "passing"
```

CI is unaffected because Part 1 Step 41's CI flow runs ONE
`dotnet-coverage collect -- dotnet test` invocation that emits a single
merged cobertura, so each assembly appears exactly once. Fix: aggregate
per-assembly across input files before applying the threshold (max
line% / branch%), or `dotnet-coverage merge` the inputs first.

## Deferred / follow-up

Everything in Part 2 §4 Phases A–F. The next single step to take is
A.3 (fix C5' threshold script) per the Phase-A → Phase-F dependency in
§5; A.2 (triage C2' failing test) is also blocking and should run in
parallel.

## Blocked-items sweep

Re-checked the two scaffold-time entries plus the previously-missed
Step 62 carry-over:

- `Host.Lease` tests → still Azurite-blocked (Phase D.2 unblocks).
- `Sender_ConfirmsDraftRequest_ReturnsPending` → still skipped (E.3).
- **NEW:** `Receiver_ApprovesPendingPackageRequest_ReturnsApproved` →
  carried over from Part 1 Step 62 with a TODO; added as a third
  Blocked Items row.

`Last re-checked = step 1` for all three.

## Obsolete-docs sweep

None — Part 2 has only this step, the scaffold INDEX, the parent
PART_2.md, and the retrospective. Scaffold INDEX + parent PART_2.md
were updated in-place to reflect this step's outputs (no obsolete
predecessors to mark).
