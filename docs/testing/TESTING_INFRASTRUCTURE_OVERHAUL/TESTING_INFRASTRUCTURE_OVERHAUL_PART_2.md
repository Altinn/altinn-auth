# Testing Infrastructure Overhaul — Part 2

Part 1 ([`TESTING_INFRASTRUCTURE_OVERHAUL_PART_1.md`](TESTING_INFRASTRUCTURE_OVERHAUL_PART_1.md))
built the shared testing infrastructure. Part 2 fills it with unit
and integration tests for the existing codebase.

Two principles:

1. **Tests are scoped by named bug classes, not by coverage %.**
   Coverage is informational. Pick test work by what bug class it
   defends against; if a step doc can't name at least one concrete
   bug class, the work probably violates principle 2.
2. **Mock-based unit tests must exercise real logic in the SUT.**
   Pure pass-through controllers and services don't earn isolation
   tests with mocks; they're covered via integration tests against
   real infrastructure (Postgres, Azurite, etc.).

Day-to-day workflow (branch naming, step-doc template, Issue
tracking, step log, blocked items): see
[`STEPS_PART_2/INDEX.md`](STEPS_PART_2/INDEX.md).

---

## Open work

Real-logic candidates remaining. Pick the next step by bug class
worth defending, not by which assembly's % is lowest. Each item is
a *category*, not a single Task — bundle related work into a
focused Task when it starts.

- ~~**Pure-logic helpers in `Altinn.AccessMgmt.Persistence.Core`
  and `Altinn.AccessMgmt.Core`.**~~ — **Substantially covered by
  steps 9–23 under [Task #2990](https://github.com/Altinn/altinn-authorization-tmp/issues/2990).**
  The pure-logic surface across the access-management code now
  has unit tests for the obvious targets (search/fuzzy
  algorithms, translation pipeline, authorization handlers,
  claim extraction, value objects, URN composition, querystring
  parsing, etc.). Re-open this category only when an audit
  identifies a new real-logic class that's reachable without
  DB / Azurite / heavy DI.
- **Live-DB repositories in `Altinn.AccessManagement.Persistence`
  and `Altinn.AccessMgmt.Persistence`.** Needs a
  `RepositoryDbCollection` xUnit collection sharing the
  `EFPostgresFactory` template-clone pattern from Part 1, then
  per-repository tests.
- **`Altinn.Authorization.Host.Pipeline` segment service.**
  `PipelineSourceService` and `PipelineSinkService` are tested
  (steps 11–12). `PipelineSegmentService.DispatchSegment` retry
  tests are deferred pending investigation of a hardcoded
  `await Task.Delay(TimeSpan.FromSeconds(2))` per consumed
  message in `EnumerateSegment` (line 48); almost certainly a
  debugging artifact left in production. Triage that delay first,
  then port the retry-test suite from `PipelineSinkServiceTest`.
- **`Altinn.Authorization.Host.Pipeline` hosted-service
  lifecycle.** `PipelineHostedService` orchestration (lease
  handling, recurring schedule, reflection-based dispatch) is
  not yet tested; needs `FeatureManager` / `ILeaseService` mock
  setup that's heavier than the pure-logic pattern.
- **`Altinn.Authorization.Host.Lease`** — blocked on an Azurite
  Testcontainers fixture. Mirror the `PostgresServer` singleton +
  `Assert.Skip` outage-guard pattern from
  `AccessMgmt.Tests/Fixtures/PostgresFixture.cs`.
- **Decide on `Altinn.Authorization.Host.Database` /
  `Altinn.Authorization.Host.MassTransit`.** Either a thin smoke
  suite or an explicit accept-the-gap (architectural glue / POCOs).
- **Mock consolidation.** 9+ near-duplicate mock/stub classes
  between `Altinn.AccessManagement.TestUtils/Mocks/` and
  `Altinn.Authorization.Tests/MockServices/` should converge into a
  shared package. Includes the `Altinn.AccessMgmt.Core.Utils.OrgUtil`
  ↔ `Altinn.AccessManagement.Api.Enterprise.Utils.OrgUtil`
  duplicate (identical real-logic, different namespace).
- **Skipped-test audit.** 16 tests across `Enduser.Api`,
  `Integration`, and `Host.Lease` are `[Skip]`ped. Decide each:
  cleanup-able vs environmentally-blocked (Azurite unblocks the
  last 2).
- **Stale xUnit v2 package pins.** `xunit` 2.9.3 +
  `xunit.runner.visualstudio` 3.1.5 in
  [`src/Directory.Packages.props`](../../src/Directory.Packages.props)
  are dead — no project sets `XUnitVersion=v2`. Remove to prevent
  accidental version-mixing.
- **Coverage-threshold reframing.** Existing
  `coverage-thresholds.json` floors should eventually be reframed
  as catastrophic-regression tripwires (current% minus a margin),
  not aspirational targets. SonarCloud's coverage exclusions for
  pass-through code is a parallel follow-up.

---

## Decision Log

Append-only. Records the *why* of plan changes so future-us can
judge whether a remembered decision still holds.

| Date | Decision | Rationale |
|---|---|---|
| 2026-04-27 | Start Part 2 instead of extending Part 1 | Part 1 plan exhausted at Step 61; new audit phase needs clean ID namespace and step-1 reset. |
| 2026-04-27 | Adopt the [`TRACKING_RETROSPECTIVE.md`](TRACKING_RETROSPECTIVE.md) tracking improvements (Phase/Tag column, YAML front matter, zero-padded filenames, short-form `// See:` comments, structural-check script as a recommended early step) | Cheaper than retrofitting mid-phase. Sidecar `steps.json` and an actual linter script deferred until a consumer motivates them. |
| 2026-04-27 (Step 5 rectification) | Replace Step 5's original cross-user-check hoist with full removal of the `AccessManagementAuthorizedParties` flag and its dead-code paths | The flag was confirmed always-on in production for some time. The originally-flagged "auth regression" in `ValidateParty_NotAsAuthenticatedUser_Forbidden` was a test-hits-dead-code artefact (the test class flipped the mocked flag to `false` mid-class). In production the legacy `else` branch was never reachable; deleting it is the right move. **No security advisory needed.** |
| 2026-04-29 (Step 7) | Realign Part 2 around bug-class-driven framing instead of per-assembly coverage targets. Close [PR apps#2978](https://github.com/Altinn/altinn-authorization-tmp/pull/2978) (Moq-only B.1) without merging; replace with DB-backed integration tests + 4 production-bug fixes on `PackageService` ([PR apps#2984](https://github.com/Altinn/altinn-authorization-tmp/pull/2984)). Drop Phase F (coverage-threshold ratchet) as written. Step-doc template now leads with `bugClassesCovered`; `coverageDelta` is informational. | Standing testing conventions adopted: (1) mock-based unit tests must exercise real logic in the SUT, not just pass-through wiring; (2) coverage % is informational, not a planning lens — pick test work by bug class; (3) tracking-doc edits ride along with code PRs, no standalone docs PRs. The existing Phase A/B/C/D/E/F structure was kept for a few weeks but then collapsed (this rewrite) once it became more drag than help. |
| 2026-04-29 (Step 10) | Collapse PART_2.md and INDEX.md to remove duplication — single realignment banner moves into the Decision Log; coverage tables removed (run `eng/testing/run-coverage.ps1` for current numbers); Phase A–F structure flattened to a thin "Open work" list; per-bug-class IDs (`C1'`, `M5'`, `L4'`) retired now that they're either resolved or replaced by named bug classes per principle 1. | The two docs had drifted into ~50 % overlap (banner, coverage table, recommended-next-steps prose). Each tracking-doc edit had to land in two places or silently diverge. The phase plan was largely vestigial — Phase A done, Phase F dropped, Phases B/C/D/E are simply "different categories of test work." Single source of truth per concern: PART_2 = audit + open work + decision log; INDEX = workflow + step log + blocked items. |
| 2026-04-30 (Steps 10–23) | Bundle pure-logic test additions under one umbrella Task ([#2990](https://github.com/Altinn/altinn-authorization-tmp/issues/2990)) instead of one Task per tested class. ~218 new tests across 14 step docs in PR [apps#3000](https://github.com/Altinn/altinn-authorization-tmp/pull/3000) covering async-iteration, pipeline source/sink services, authorization handlers, claim extraction, translation pipeline, value objects, query inputs, and URN composition. | The granularity rule says Tasks bundle multi-PR work; one Task per tested class produces visible noise on the team backlog without proportional value. Two Tasks ([#2997](https://github.com/Altinn/altinn-authorization-tmp/issues/2997) and [#2998](https://github.com/Altinn/altinn-authorization-tmp/issues/2998)) were created on this rule's old reading and folded into #2990 mid-sweep. Going forward, all Part 2 pure-logic test additions land under #2990 as separate step docs + commits, not as separate Issues. |

---

*The full pre-Step-10 audit (test-project inventory, fixture
inventory, mock inventory, coverage baselines, Phase A–F plan, and
the resolved `C1'`–`L5'` issue list) is preserved in git history;
see commit `2c64ce4a` and prior. Removed from the live doc to cut
maintenance drag — most of it described state that no longer holds
or planning lenses we no longer use.*
