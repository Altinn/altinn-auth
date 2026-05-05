---
step: 10
title: PaginatorStream unit tests + Part 1 comment sweep + tracking-doc collapse
phase: B/DOC
status: complete
linkedIssues:
  task: 2990
bugClassesCovered:
  - "Iterator continues after first unsuccessful response instead of yielding error and breaking — would silently retry forever or skip the error entirely"
  - "Termination broken if Links.Next handling regresses to checking only null OR only empty (current: IsNullOrEmpty)"
  - "Next-page request targets a stale URI from newRequest instead of Links.Next — would loop on the same page"
  - "Pre-cancelled CancellationToken not honored — would issue at least one request before checking"
  - "Prior HttpResponseMessage leaks (not disposed) before fetching the next page"
verifiedTests: 7
touchedFiles: 7
---

# Step 10 — `PaginatorStream<T>` unit tests + Part 1 comment sweep + doc collapse

## Goal

Pin down `PaginatorStream<T>` iteration behavior with pure unit
tests, finish the Part 1 → short-form `// See:` comment sweep, and
collapse the duplication between
[`TESTING_INFRASTRUCTURE_OVERHAUL_PART_2.md`](../TESTING_INFRASTRUCTURE_OVERHAUL_PART_2.md)
and this `INDEX.md` that had grown to ~50 % overlap.

`PaginatorStream<T>` is real-logic per the *"real logic vs
pass-through wiring"* filter — async-iterator state machine over
HTTP responses, follows `Links.Next`, terminates on null/empty,
yields errors and breaks. The class is otherwise exercised only
via live external Register / ResourceRegister calls
(`PartiesStreamEndpointTest`, etc.) which `SkipIfMissingConfiguration`
in CI without those env configs — so the iterator behavior was
unprotected.

## What changed

### Tests added

[`src/libs/Altinn.Authorization.Integration/test/Altinn.Authorization.Integration.Tests/PaginatorStreamTest.cs`](../../../src/libs/Altinn.Authorization.Integration/test/Altinn.Authorization.Integration.Tests/PaginatorStreamTest.cs)
— 7 tests covering:

- `FirstResponseUnsuccessful_YieldsErrorAndStops` — initial 404
  yields one error response, no fetcher invoked.
- `LinksNextNullOrEmpty_TerminatesAfterFirstPage` — `[Theory]` over
  `null` and `""`; pins both branches of `IsNullOrEmpty`.
- `FollowsLinksNext_FetchesSubsequentPagesUntilNullNext` — three
  pages, verifies captured URIs match the `Links.Next` chain.
- `UnsuccessfulPageMidStream_YieldsErrorAndStops` — page 1 OK,
  page 2 500; iterator yields both then breaks.
- `PreCancelledToken_YieldsNothing` — `WithCancellation` over a
  pre-cancelled token; loop exits on first condition check.
- `PriorResponseDisposed_AfterAdvancingToNextPage` — subclasses
  `HttpResponseMessage` to track `Dispose()`; verifies the prior
  response is disposed once the consumer advances.

Test harness: a `CapturingHandler : HttpMessageHandler` records
captured request URIs and dequeues seeded responses; a
`TrackingResponse : HttpResponseMessage` exposes `Disposed`. No
external dependencies.

### Comment sweep

Three test files migrated from the long-form Part 1 path reference
to the short form per the source-code cross-reference convention:

- [`AccessMgmt.Tests/Controllers/DelegationsControllerTest.cs:30`](../../../src/apps/Altinn.AccessManagement/test/AccessMgmt.Tests/Controllers/DelegationsControllerTest.cs:30)
- [`AccessMgmt.Tests/Controllers/MaskinportenSchemaControllerTest.cs:42`](../../../src/apps/Altinn.AccessManagement/test/AccessMgmt.Tests/Controllers/MaskinportenSchemaControllerTest.cs:42)
- [`AccessMgmt.Tests/Controllers/ResourceOwnerAPI/AppsInstanceDelegationControllerTest.cs:30`](../../../src/apps/Altinn.AccessManagement/test/AccessMgmt.Tests/Controllers/ResourceOwnerAPI/AppsInstanceDelegationControllerTest.cs:30)

All three referenced
`STEPS_PART_1/AccessMgmt_WAF_Consolidation_Plan_and_POC.md` (step
16). New form: `// See: overhaul part-1 step 16`.

### Tracking-doc collapse

PART_2.md and INDEX.md had drifted into ~50 % overlap (realignment
banner duplicated, coverage table duplicated, recommended-next-steps
prose mirroring §4 phase plan). Each tracking-doc edit had to land
in two places or silently diverge.

- [`TESTING_INFRASTRUCTURE_OVERHAUL_PART_2.md`](../TESTING_INFRASTRUCTURE_OVERHAUL_PART_2.md)
  rewritten: cut ~85 % of the file. Removed §1 (audit detail), §3
  (best practices — Part 1 codified them), §5 (execution-order
  diagram), §6 (cross-ref to INDEX). Replaced §2 + §4 with a thin
  "Open work" list. Decision Log preserved + appended.
- [`STEPS_PART_2/INDEX.md`](INDEX.md) rewritten: cut realignment
  banner, "Final Coverage" table, and the 21-item Recommended Next
  Steps list. Trimmed step-log prose to one-line topics per row.
  Workflow conventions (step-doc template, branch naming, GitHub
  Issues, source-code refs) preserved.

The full pre-collapse snapshot lives in git history at the merge
commit of [PR apps#2989](https://github.com/Altinn/altinn-authorization-tmp/pull/2989)
(`2c64ce4a`) and prior.

## Verification

```text
$ dotnet test src/libs/Altinn.Authorization.Integration/test/Altinn.Authorization.Integration.Tests/...
Passed!  - Failed: 0, Passed: 47, Skipped: 5, Total: 52
```

PaginatorStream filter: 7 passed, 0 failed. Pre-existing 5 skips
are the live Register / ResourceRegister tests gated by
`SkipIfMissingConfiguration`; unchanged by this step.

Comment sweep: `grep "// See docs/testing"` over `*.cs` returns
empty — no remaining long-form references in source.

## Deferred / follow-up

Bug classes spotted but **not** fixed (would broaden scope; left
for a future step or PR):

- `PaginatorStream<T>.GetAsyncEnumerator` dereferences
  `content.Links.Next` directly — NRE if a successful response
  body has `links: null`. Not pinned by tests because behavior is
  a crash, and pinning a crash isn't a useful contract.
- Unsuccessful-path response disposal: in the error branch
  (`yield return response; yield break;`) the
  `responseIterator` is yielded to the caller but never explicitly
  disposed — ownership transfer is implicit. Not necessarily a
  leak depending on consumer semantics, but ambiguous.

Both are worth addressing in a follow-up Task that fixes the
behavior + adds the tests in one PR (precedent: Step 7's PR
[apps#2984](https://github.com/Altinn/altinn-authorization-tmp/pull/2984)
fix-with-tests pattern).
