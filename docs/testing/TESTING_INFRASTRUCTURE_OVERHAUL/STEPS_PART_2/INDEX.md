# Part 2 Step Log

Workflow tracker for the Part 2 testing-infrastructure overhaul.
Audit, open work, and decision history live in
[`../TESTING_INFRASTRUCTURE_OVERHAUL_PART_2.md`](../TESTING_INFRASTRUCTURE_OVERHAUL_PART_2.md).

## Read order (fresh chat)

1. This file — workflow rules, step log, blocked items.
2. [`../TESTING_INFRASTRUCTURE_OVERHAUL_PART_2.md`](../TESTING_INFRASTRUCTURE_OVERHAUL_PART_2.md)
   § Open work + § Decision Log.
3. The step doc for the work you're about to do (linked in the
   step log table below).

---

## Conventions

### Step doc template

Every step doc starts with this YAML, followed by Goal / What
changed / Verification / Deferred sections:

```yaml
---
step: <N>
title: <title>
phase: <A|B|C|D|E|CI|DOC|FIX>
status: complete | partial | blocked | reverted
linkedIssues:
  task: <issue#>
bugClassesCovered:
  - "<named bug class>"
verifiedTests: <count>
touchedFiles: <count>
---
```

`bugClassesCovered` is the **lead field**. If a step can't name at
least one concrete bug class its tests defend against, the work
probably violates the real-logic rule and shouldn't merge. Steps
that produce no new test coverage (tooling, dead-code removal, doc
revisions) can use `bugClassesCovered: []`.

### Step doc filenames

`<NN>_<Name>.md`, zero-padded so lexicographic order matches
chronological.

### Source-code cross-references

```csharp
// See: overhaul part-2 step 7
// See: overhaul part-1 step 17
```

This file is the single place that maps "part-N step M" → the
actual file path. When the folder is reorganized, only that
mapping needs updating.

### Branch naming

`feat/<task-issue#>_<short_name_with_underscores>` — branched from
`main`. No `claude/` prefix, no random codenames. One branch per
Task. Rename harness-default branches before pushing.

### GitHub Issues

Type `Task` (Issue Type, not label). Created **just-in-time** when
work starts, not upfront from the open-work list. Assignee
`howieandersen`, label `Backend`; add to project 50 ("Team
Tilgangsstyring & Kontroll") with current Sprint, Start = today,
Status = 👷In Progress. On close: Stop = today, Status = ✅ Done.

Bundle small Tasks into one PR (`Closes #X, #Y, #Z`) rather than
opening a PR per Task. PR title and body describe the change in
plain English — no internal audit IDs, no references to this file
or the audit doc.

### Step completion

1. Write the step doc.
2. Append a row to the [Step log](#step-log) below.
3. Run impacted tests; record results in the doc.
4. Re-check [Blocked items](#blocked-items); refresh
   `Last re-checked` even if nothing changed.
5. Commit (granular, push to working branch). Open a PR only when
   a coherent bundle is ready for review.

---

## Step log

| # | Completed | Phase | Topic | Doc |
|---|---|---|---|---|
| 1 | 2026-04-27 | KICKOFF | Part 2 kickoff audit (test inventory, fixtures, coverage baseline, C1'–L5' issue list) | [01](01_Part_2_Kickoff_Audit.md) |
| 2 | 2026-04-27 | A | Fix `check-coverage-thresholds.ps1` workstation false-positives (C5') | [02](02_Fix_Coverage_Threshold_Aggregation.md) |
| 3 | 2026-04-27 | A | Delete empty `Altinn.Authorization.ABAC.Tests` project (C1') | [03](03_Delete_Empty_ABAC_Tests.md) |
| 4 | 2026-04-27 | A | Scaffold `Altinn.Authorization.Host.Pipeline.Tests` (C3' / A.5) | [04](04_Scaffold_Host_Pipeline_Tests.md) |
| 5 | 2026-04-27 | A | Remove `AccessManagementAuthorizedParties` flag + dead `else` branches; failing test passes naturally (C2') | [05](05_Remove_AccessManagementAuthorizedParties_Flag.md) |
| 6 | 2026-04-27 | A | T1 closing sweep + coverage baseline refresh against post-C5'-fix merged cobertura | [06](06_T1_Closing_Sweep_and_Baseline_Refresh.md) |
| 7 | 2026-04-29 | B | Metadata API DB-backed integration tests + 4 production fixes in `PackageService` (PR [apps#2984](https://github.com/Altinn/altinn-authorization-tmp/pull/2984), #2983) | (PR body) |
| 8 | 2026-04-29 | CI | Fix `Report-failed-tests` MTP-log path lookup (PR [apps#2987](https://github.com/Altinn/altinn-authorization-tmp/pull/2987), #2986) | (PR body) |
| 9 | 2026-04-29 | B | `SearchPropertyBuilder<T>` pure-logic unit tests (PR [apps#2989](https://github.com/Altinn/altinn-authorization-tmp/pull/2989), #2988) | [09](09_SearchPropertyBuilder_Tests_And_Realignment.md) |
| 10 | 2026-04-29 | B/DOC | `PaginatorStream<T>` unit tests + Part 1 `// See:` comment sweep + collapse PART_2/INDEX duplication (#2990) | [10](10_PaginatorStream_Tests_And_Doc_Collapse.md) |
| 11 | 2026-04-30 | B | `PipelineSourceService` async-enumerator unit tests (#2990) | [11](11_PipelineSourceService_Tests.md) |
| 12 | 2026-04-30 | B | `PipelineSinkService` retry-semantics unit tests (#2990) | [12](12_PipelineSinkService_Tests.md) |
| 13 | 2026-04-30 | B | `AccessMgmt.Core.Authorization` unit tests — `ConditionalScope`, `ScopeConditionAuthorizationHandler`, `DefaultAuthorizationScopeProvider` (#2990) | [13](13_AccessMgmt_Core_Authorization_Tests.md) |
| 14 | 2026-04-30 | B | `TranslationExtensions` null-short-circuit + service-delegation unit tests (#2990) | [14](14_TranslationExtensions_Tests.md) |
| 15 | 2026-04-30 | B | `TranslationMiddleware` (q-value parsing + normalization) + `ControllerExtensions` (language-code resolution) unit tests (#2990) | [15](15_TranslationMiddleware_And_ControllerExtensions_Tests.md) |
| 16 | 2026-04-30 | B | `DeepTranslationExtensions` recursion-topology unit tests — pins null short-circuits, AreaDto cycle avoidance, asymmetric Provider/Type recursion (#2990) | [16](16_DeepTranslationExtensions_Tests.md) |
| 17 | 2026-04-30 | B | `AuthenticationHelper` (claim extraction) + `JwtTokenUtil` (cookie/Bearer resolution) unit tests (#2990) | [17](17_AuthenticationHelper_And_JwtTokenUtil_Tests.md) |
| 18 | 2026-04-30 | B | `MaskinportenSchemaAuthorizer` delegation-lookup auth-decision unit tests (#2990) | [18](18_MaskinportenSchemaAuthorizer_Tests.md) |
| 19 | 2026-04-30 | B | `Api.Internal.UserUtil` party-uuid claim extraction unit tests (#2990) | [19](19_UserUtil_Tests.md) |
| 20 | 2026-04-30 | B | `PersonIdentifier` SSN modulo-11 validation + equality + JSON round-trip unit tests (#2990) | [20](20_PersonIdentifier_Tests.md) |
| 21 | 2026-04-30 | B | `ConnectionQueryFilter.HasAny` + default-flag pin — gates connection scans; pins which collections count and which intentionally don't (#2990) | [21](21_ConnectionQueryFilter_Tests.md) |
| 22 | 2026-04-30 | B | Api.Enduser `DecisionHelper` accessor + PDP-decision validation unit tests — auth-bypass / DoS bug classes (#2990) | [22](22_DecisionHelper_Tests.md) |
| 23 | 2026-04-30 | B | `BaseUrn` URN-string composition unit tests — pins lowercase + correct nesting at every level (#2990) | [23](23_BaseUrn_Tests.md) |

---

## Blocked items

`Last re-checked` is the most recent step that revisited the row.
Refresh it as part of every step's blocked-items sweep, even if
nothing changed.

| Item | Blocker | Notes | Last re-checked |
|---|---|---|---|
| `Altinn.Authorization.Host.Lease` tests | Azurite / Azure Storage Emulator required | 2 tests `[Skip]`ped; assembly at 6.87% line. Unblocked once an Azurite Testcontainers fixture lands. | step 23 |
| `Sender_ConfirmsDraftRequest_ReturnsPending` | Environmental investigation needed | `[Skip]`ped during Part 1 Step 51 after the `ResourceRegistryMock` cache-hit fix. Will be reviewed under skipped-test audit. | step 23 |
| `Receiver_ApprovesPendingPackageRequest_ReturnsApproved` | Fixture mis-seed — needs rewrite | `[Skip]`ped during Part 1 Step 62 with a TODO (auth as MD of receiver + pre-existing Rightholder connection). | step 23 |

---

## Recommended next

Pick from
[`../TESTING_INFRASTRUCTURE_OVERHAUL_PART_2.md` § Open work](../TESTING_INFRASTRUCTURE_OVERHAUL_PART_2.md#open-work).
Apply the real-logic-vs-pass-through filter and name the bug
classes the new tests defend against in the step-doc front matter.

---

## Splitting parts

When the step log approaches **~50 entries**, close Part 2 and
start Part 3 (`STEPS_PART_3/`,
`TESTING_INFRASTRUCTURE_OVERHAUL_PART_3.md`). Part 1 ended at 61
steps; tables that long are unreadable. Part numbers are cheap.
